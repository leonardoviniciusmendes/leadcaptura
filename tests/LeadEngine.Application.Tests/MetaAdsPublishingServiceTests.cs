using System.Net;
using System.Diagnostics;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class MetaAdsPublishingServiceTests
{
    [Fact]
    public async Task Retry_FalhaParcialCriandoCreative_ReutilizaCampaignAdSetETentaCreative()
    {
        var ctx = TestContext.Create();
        ctx.PreviewReady = false;

        var result = await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Equal("Concluida", result.Status);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(1, ctx.Graph.CreativeCreates);
        Assert.Equal(1, ctx.Graph.AdCreates);
        Assert.Equal("120249268268550352", ctx.Publicacao.CampaignExternalId);
        Assert.Equal("120249268268890352", ctx.Publicacao.AdSetExternalId);
        Assert.Equal("creative_1", ctx.Publicacao.CreativeExternalId);
        Assert.Equal("ad_1", ctx.Publicacao.AdExternalId);
        Assert.Equal("page_1", ctx.Graph.LastCreativePayload?.PageId);
        Assert.Equal("image_hash_1", ctx.Graph.LastCreativePayload?.ImageHash);
    }

    [Fact]
    public async Task Retry_QuandoCreativeCriado_ContinuaParaCriacaoDoAd()
    {
        var ctx = TestContext.Create();

        await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Single(ctx.Graph.CallOrder, x => x == "creative");
        Assert.Single(ctx.Graph.CallOrder, x => x == "ad");
        Assert.True(ctx.Graph.CallOrder.IndexOf("creative") < ctx.Graph.CallOrder.IndexOf("ad"));
    }

    [Fact]
    public async Task Retry_ErroMetaNoCreative_MantemFalhaParcialIdsEDetalhes()
    {
        var ctx = TestContext.Create();
        ctx.Graph.CreativeException = CreativeInvalidException();

        var result = await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Equal("FalhaParcial", result.Status);
        Assert.Equal("CriandoCreative", result.UltimaEtapaConcluida);
        Assert.Equal("120249268268550352", result.CampaignExternalId);
        Assert.Equal("120249268268890352", result.AdSetExternalId);
        Assert.Null(result.CreativeExternalId);
        Assert.Null(result.AdExternalId);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(1, ctx.Graph.CreativeCreates);
        Assert.Equal(0, ctx.Graph.AdCreates);
        Assert.Equal(StatusPublicacaoMetaAds.FalhaParcial, ctx.Publicacao.Status);
        Assert.Equal("CriandoCreative", ctx.Publicacao.UltimaEtapaConcluida);
        Assert.Contains("HTTP=BadRequest", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("message=Invalid parameter", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("type=OAuthException", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("code=100", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("error_subcode=1885183", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("error_user_title=Creative invalido", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("error_user_msg=Campo image_hash rejeitado", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("fbtrace_id=trace123", ctx.Publicacao.UltimoErroMensagem);
    }

    [Fact]
    public async Task Retry_ErroMetaNoCreative_TokenOriginalCancelado_PersisteFalhaComTokenIndependente()
    {
        var ctx = TestContext.Create();
        using var requestCts = new CancellationTokenSource();
        ctx.Graph.CancelBeforeCreativeException = requestCts;
        ctx.Graph.CreativeException = CreativeInvalidException();

        var result = await Service(ctx).RetentarAsync(ctx.Publicacao.Id, requestCts.Token);

        Assert.True(requestCts.IsCancellationRequested);
        Assert.Equal("FalhaParcial", result.Status);
        Assert.Equal("CriandoCreative", result.UltimaEtapaConcluida);
        Assert.Equal("120249268268550352", result.CampaignExternalId);
        Assert.Equal("120249268268890352", result.AdSetExternalId);
        Assert.Null(result.CreativeExternalId);
        Assert.Null(result.AdExternalId);
        Assert.False(ctx.Publicacoes.FailureSaveTokenWasCancellationRequested);
        Assert.True(ctx.Publicacoes.FailureSaveTokenCanBeCanceled);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(1, ctx.Graph.CreativeCreates);
        Assert.Equal(0, ctx.Graph.AdCreates);
        Assert.Contains("message=Invalid parameter", ctx.Publicacao.UltimoErroMensagem);
        Assert.Contains("fbtrace_id=trace123", ctx.Publicacao.UltimoErroMensagem);
    }

    [Fact]
    public async Task Retry_ErroMetaNoCreative_PersistenciaDeFalhaTemTimeoutProprio()
    {
        var ctx = TestContext.Create();
        ctx.Graph.CreativeException = CreativeInvalidException();
        ctx.Publicacoes.WaitForFailurePersistenceTimeout = true;

        var elapsed = Stopwatch.StartNew();
        await Assert.ThrowsAsync<TaskCanceledException>(() => Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None));
        elapsed.Stop();

        Assert.True(ctx.Publicacoes.FailurePersistenceTimedOut);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(10));
        Assert.Equal(StatusPublicacaoMetaAds.FalhaParcial, ctx.Publicacao.Status);
        Assert.Equal("CriandoCreative", ctx.Publicacao.UltimaEtapaConcluida);
        Assert.Equal("120249268268550352", ctx.Publicacao.CampaignExternalId);
        Assert.Equal("120249268268890352", ctx.Publicacao.AdSetExternalId);
        Assert.Contains("message=Invalid parameter", ctx.Publicacao.UltimoErroMensagem);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
    }

    [Fact]
    public async Task Retry_BuildCreativeInvalido_NaoPermaneceCriandoCreative()
    {
        var ctx = TestContext.Create();
        ctx.PreviewPageId = null;
        ctx.Publicacao.Status = StatusPublicacaoMetaAds.AdSetCriado;
        ctx.Publicacao.UltimaEtapaConcluida = "AdSetCriado";

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None));

        Assert.Equal(StatusPublicacaoMetaAds.FalhaParcial, ctx.Publicacao.Status);
        Assert.Equal("CriandoCreative", ctx.Publicacao.UltimaEtapaConcluida);
        Assert.Equal("invalid_operation", ctx.Publicacao.UltimoErroCodigo);
        Assert.Equal("120249268268550352", ctx.Publicacao.CampaignExternalId);
        Assert.Equal("120249268268890352", ctx.Publicacao.AdSetExternalId);
        Assert.Null(ctx.Publicacao.CreativeExternalId);
        Assert.Null(ctx.Publicacao.AdExternalId);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(0, ctx.Graph.CreativeCreates);
        Assert.Equal(0, ctx.Graph.AdCreates);
    }

    [Fact]
    public async Task Retry_CreativeCriado_SalvaCreativeExternalIdAntesDeCriarAd()
    {
        var ctx = TestContext.Create();
        ctx.Graph.RequireCreativeIdPersistedBeforeAd = true;

        var result = await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Equal("Concluida", result.Status);
        Assert.True(ctx.Graph.CreativeIdWasPersistedBeforeAd);
        Assert.Equal("creative_1", ctx.Publicacao.CreativeExternalId);
        Assert.Equal("ad_1", ctx.Publicacao.AdExternalId);
    }

    [Fact]
    public async Task Retry_ProcessoReiniciadoAposCreativeCriado_NaoDuplicaCreative()
    {
        var ctx = TestContext.Create();
        ctx.Publicacao.Status = StatusPublicacaoMetaAds.CreativeCriado;
        ctx.Publicacao.UltimaEtapaConcluida = "CreativeCriado";
        ctx.Publicacao.CreativeExternalId = "creative_existente";

        var result = await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Equal("Concluida", result.Status);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(0, ctx.Graph.CreativeCreates);
        Assert.Equal(1, ctx.Graph.AdCreates);
        Assert.Equal("creative_existente", ctx.Publicacao.CreativeExternalId);
        Assert.Equal("ad_1", ctx.Publicacao.AdExternalId);
    }

    [Fact]
    public async Task Retry_PublicacaoJaConcluida_NaoDuplicaRecursos()
    {
        var ctx = TestContext.Create();
        ctx.Publicacao.Status = StatusPublicacaoMetaAds.Concluida;
        ctx.Publicacao.CreativeExternalId = "creative_1";
        ctx.Publicacao.AdExternalId = "ad_1";

        await Service(ctx).RetentarAsync(ctx.Publicacao.Id, CancellationToken.None);

        Assert.Empty(ctx.Graph.CallOrder);
        Assert.Equal(0, ctx.Graph.CampaignCreates);
        Assert.Equal(0, ctx.Graph.AdSetCreates);
        Assert.Equal(0, ctx.Graph.CreativeCreates);
        Assert.Equal(0, ctx.Graph.AdCreates);
    }

    private static MetaAdsPublishingService Service(TestContext ctx) => new(
        ctx.Campanhas,
        ctx.Contas,
        ctx.Selecoes,
        ctx.Publicacoes,
        ctx.Graph,
        ctx.Preview,
        ctx.Resolver,
        ctx.Protector,
        NullLogger<MetaAdsPublishingService>.Instance);

    private static MetaAdsGraphApiException CreativeInvalidException() => new(
        "Invalid parameter",
        "100",
        false,
        HttpStatusCode.BadRequest,
        "1885183",
        "OAuthException",
        "trace123",
        "Invalid parameter",
        "Creative invalido",
        "Campo image_hash rejeitado",
        "{\"blame_field\":\"image_hash\"}",
        "[\"object_story_spec\",\"link_data\",\"image_hash\"]",
        null,
        false);

    private sealed class TestContext
    {
        public MetaAdsConta Conta { get; } = new() { Id = Guid.NewGuid(), Ativa = true, AccessTokenProtegido = "token" };
        public MetaAdsAtivoSelecionado Selecao { get; }
        public MetaAdsPublicacao Publicacao { get; }
        public bool PreviewReady { get; set; } = true;
        public string? PreviewPageId { get; set; } = "page_1";
        public string? PreviewImageHash { get; set; } = "image_hash_1";
        public Campanhas Campanhas { get; }
        public Contas Contas { get; }
        public Selecoes Selecoes { get; }
        public Publicacoes Publicacoes { get; }
        public Graph Graph { get; } = new();
        public Preview Preview { get; }
        public Resolver Resolver { get; } = new();
        public Protector Protector { get; } = new();

        private TestContext()
        {
            Selecao = new MetaAdsAtivoSelecionado { Id = Guid.NewGuid(), MetaAdsContaId = Conta.Id, AdAccountId = "act_1" };
            Publicacao = new MetaAdsPublicacao
            {
                Id = Guid.NewGuid(),
                CampanhaId = Guid.NewGuid(),
                MetaAdsContaId = Conta.Id,
                AdAccountId = "act_1",
                Status = StatusPublicacaoMetaAds.FalhaParcial,
                UltimaEtapaConcluida = "CriandoCreative",
                CampaignExternalId = "120249268268550352",
                AdSetExternalId = "120249268268890352",
                DataInicio = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };
            Campanhas = new Campanhas(Publicacao.CampanhaId);
            Contas = new Contas(Conta);
            Selecoes = new Selecoes(Selecao);
            Publicacoes = new Publicacoes(Publicacao);
            Preview = new Preview(this);
            Graph.Context = this;
        }

        public static TestContext Create() => new();
    }

    private sealed class Preview(TestContext ctx) : IMetaAdsPreviewService
    {
        public Task<MetaAdsPreviewResponse> GerarAsync(MetaAdsPreviewRequest request, CancellationToken cancellationToken)
        {
            var preflight = new MetaAdsPreflight(ctx.PreviewReady, ctx.PreviewReady ? [] : [new MetaAdsPreflightItem("DiagnosticOnly", "ERROR", "Bloqueio que nao deve impedir retomada parcial.")]);
            return Task.FromResult(new MetaAdsPreviewResponse(
                request.CampanhaId,
                new MetaAdsPreviewAssets(null, null, "act_1", null, ctx.PreviewPageId, null, null, null, null, null),
                new MetaAdsCampaignPreview("Campanha", "OUTCOME_TRAFFIC", "PAUSED", "NONE", ["NONE"]),
                new MetaAdsAdSetPreview("AdSet", "OUTCOME_TRAFFIC", 20m, 2000, "BRL", "IMPRESSIONS", "LINK_CLICKS", "LOWEST_COST_WITHOUT_CAP", new MetaAdsTargetingPreview(["BR"], new MetaAdsLocationResponse("1001655", "Rio", "city", "BR", "Brazil", "RJ", null, false, true), null, null, 18, 65), null, null, null),
                new MetaAdsCreativePreview(ctx.PreviewPageId, null, "Texto principal", "Headline", "Descricao", "https://example.com", "LEARN_MORE", null, "hash", ctx.PreviewImageHash, !string.IsNullOrWhiteSpace(ctx.PreviewImageHash)),
                new MetaAdsAdPreview("Ad", "PAUSED"),
                preflight));
        }
    }

    private sealed class Graph : IMetaAdsGraphClient
    {
        public int CampaignCreates { get; private set; }
        public int AdSetCreates { get; private set; }
        public int CreativeCreates { get; private set; }
        public int AdCreates { get; private set; }
        public List<string> CallOrder { get; } = [];
        public MetaAdsGraphApiException? CreativeException { get; set; }
        public CancellationTokenSource? CancelBeforeCreativeException { get; set; }
        public MetaAdsCreativeCreatePayload? LastCreativePayload { get; private set; }
        public bool RequireCreativeIdPersistedBeforeAd { get; set; }
        public bool CreativeIdWasPersistedBeforeAd { get; private set; }
        public TestContext? Context { get; set; }
        public Task<bool> ResourceExistsAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<MetaAdsCreateResult> CreateCampaignAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCampaignCreatePayload payload, CancellationToken cancellationToken) { CampaignCreates++; CallOrder.Add("campaign"); return Task.FromResult(new MetaAdsCreateResult("campaign_1")); }
        public Task<MetaAdsCreateResult> CreateAdSetAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdSetCreatePayload payload, CancellationToken cancellationToken) { AdSetCreates++; CallOrder.Add("adset"); return Task.FromResult(new MetaAdsCreateResult("adset_1")); }
        public Task<MetaAdsCreateResult> CreateAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCreativeCreatePayload payload, CancellationToken cancellationToken)
        {
            CreativeCreates++;
            CallOrder.Add("creative");
            LastCreativePayload = payload;
            if (CreativeException is not null)
            {
                CancelBeforeCreativeException?.Cancel();
                throw CreativeException;
            }
            return Task.FromResult(new MetaAdsCreateResult("creative_1"));
        }
        public Task<MetaAdsCreateResult> CreateAdAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdCreatePayload payload, CancellationToken cancellationToken)
        {
            AdCreates++;
            CallOrder.Add("ad");
            if (RequireCreativeIdPersistedBeforeAd)
            {
                CreativeIdWasPersistedBeforeAd = Context?.Publicacao.CreativeExternalId == payload.CreativeId && !string.IsNullOrWhiteSpace(payload.CreativeId);
            }
            return Task.FromResult(new MetaAdsCreateResult("ad_1"));
        }
        public Task<MetaAdAccountDto> GetAdAccountAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCreativeDto>> GetAdCreativesAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsBusinessResponse>> ListBusinessesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsAdAccountResponse>> ListAdAccountsAsync(MetaAdsConfiguration config, string accessToken, string businessId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsPageResponse>> ListPagesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsPageResponse?> GetPageAsync(MetaAdsConfiguration config, string accessToken, string pageId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsPixelResponse>> ListPixelsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsPermissionStatusResponse> GetPermissionsAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsLocationResponse>> SearchTargetingLocationsAsync(MetaAdsConfiguration config, string accessToken, string query, string countryCode, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> UploadAdImageAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteCampaignAsync(MetaAdsConfiguration config, string accessToken, string campaignId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdSetAsync(MetaAdsConfiguration config, string accessToken, string adSetId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateDiagnosticAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsDiagnosticCreativeCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string creativeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdAsync(MetaAdsConfiguration config, string accessToken, string adId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Publicacoes(MetaAdsPublicacao publicacao) : IMetaAdsPublicacaoRepository
    {
        public bool? FailureSaveTokenWasCancellationRequested { get; private set; }
        public bool? FailureSaveTokenCanBeCanceled { get; private set; }
        public bool WaitForFailurePersistenceTimeout { get; set; }
        public bool FailurePersistenceTimedOut { get; private set; }
        public Task<MetaAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == publicacao.Id ? publicacao : null);
        public Task<MetaAdsPublicacao?> ObterPorCampanhaAdAccountAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken) => Task.FromResult(campanhaId == publicacao.CampanhaId && adAccountId == publicacao.AdAccountId ? publicacao : null);
        public Task AdicionarAsync(MetaAdsPublicacao publicacao, CancellationToken cancellationToken) => Task.CompletedTask;
        public async Task SalvarAsync(CancellationToken cancellationToken)
        {
            if (IsFailureSave())
            {
                FailureSaveTokenWasCancellationRequested = cancellationToken.IsCancellationRequested;
                FailureSaveTokenCanBeCanceled = cancellationToken.CanBeCanceled;
                if (WaitForFailurePersistenceTimeout)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        FailurePersistenceTimedOut = true;
                        throw;
                    }
                }
            }
        }

        private bool IsFailureSave()
        {
            return publicacao.Status is StatusPublicacaoMetaAds.FalhaParcial or StatusPublicacaoMetaAds.Falha or StatusPublicacaoMetaAds.EstadoIndeterminado or StatusPublicacaoMetaAds.Inconsistente
                && !string.IsNullOrWhiteSpace(publicacao.UltimoErroCodigo);
        }
    }

    private sealed class Contas(MetaAdsConta conta) : IMetaAdsContaRepository
    {
        public Task<MetaAdsConta?> ObterAtivaAsync(CancellationToken cancellationToken) => Task.FromResult<MetaAdsConta?>(conta);
        public Task<MetaAdsConta?> ObterPorMetaUserIdAsync(string metaUserId, CancellationToken cancellationToken) => Task.FromResult<MetaAdsConta?>(null);
        public Task AdicionarAsync(MetaAdsConta conta, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Selecoes(MetaAdsAtivoSelecionado selecao) : IMetaAdsAtivoSelecionadoRepository
    {
        public Task<MetaAdsAtivoSelecionado?> ObterPorContaIdAsync(Guid contaId, CancellationToken cancellationToken) => Task.FromResult(contaId == selecao.MetaAdsContaId ? selecao : null);
        public Task AdicionarAsync(MetaAdsAtivoSelecionado selecao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Campanhas(Guid campanhaId) : ICampanhaRepository
    {
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == campanhaId ? new Campanha { Id = campanhaId } : null);
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<Campanha?>(null);
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]);
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>([]);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Resolver : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken) => Task.FromResult(new ResolvedConfigurationValue(chave switch
        {
            "GraphApiBaseUrl" => "https://graph.facebook.com",
            "GraphApiVersion" => "v23.0",
            _ => string.Empty
        }, true, OrigemConfiguracao.Padrao, false));
        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Protector : ISecretProtector
    {
        public string Protect(string value) => value;
        public string Unprotect(string protectedValue) => protectedValue;
    }
}
