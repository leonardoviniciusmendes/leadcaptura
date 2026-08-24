using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsSynchronizationTests
{
    [Fact]
    public async Task Ativar_BloqueiaPublicacaoNaoReconciliada()
    {
        var ctx = Context();
        ctx.Publicacao.Status = StatusPublicacaoGoogleAds.Publicada;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(ctx).AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None));

        Assert.Contains("reconciliada", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(ctx.Query.StatusCalls);
    }

    [Fact]
    public async Task Ativar_BloqueiaFlagDesabilitada()
    {
        var ctx = Context(allowActivation: false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(ctx).AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None));

        Assert.Contains("desabilitada", ex.Message);
        Assert.Empty(ctx.Query.StatusCalls);
    }

    [Fact]
    public async Task Ativar_BloqueiaContaErrada()
    {
        var ctx = Context(testCustomerId: "1112223333");

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(ctx).AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None));

        Assert.Contains("CustomerId", ex.Message);
        Assert.Empty(ctx.Query.StatusCalls);
    }

    [Fact]
    public async Task Ativar_BloqueiaConfirmacaoAusente()
    {
        var ctx = Context();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).AtivarAsync(ctx.Publicacao.Id, new GoogleAdsStatusActionRequest(false), CancellationToken.None));

        Assert.Contains("Confirme", ex.Message);
        Assert.Empty(ctx.Query.StatusCalls);
    }

    [Fact]
    public async Task Ativar_AlteraSomenteRecursosDaPublicacaoComEnabled()
    {
        var ctx = Context();
        ctx.Publicacao.Recursos.Add(new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), GoogleAdsPublicacaoId = ctx.Publicacao.Id, TipoRecurso = "Budget", ResourceName = "customers/9890172254/campaignBudgets/1", Status = "PAUSED" });
        ctx.Publicacao.Recursos.Add(new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), GoogleAdsPublicacaoId = ctx.Publicacao.Id, TipoRecurso = "CampaignCriterion", ResourceName = "customers/9890172254/campaignCriteria/1", Status = "PAUSED" });

        var result = await Service(ctx).AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None);

        Assert.Equal(StatusPublicacaoGoogleAds.Reconciliada, result.Status);
        Assert.Single(ctx.Query.StatusCalls);
        Assert.Equal("ENABLED", ctx.Query.StatusCalls[0].Status);
        Assert.Equal(["AdGroup", "Keyword", "Keyword", "ResponsiveSearchAd", "Campaign"], ctx.Query.StatusCalls[0].Resources.Select(x => x.TipoRecurso).ToArray());
        Assert.DoesNotContain(ctx.Query.StatusCalls[0].Resources, x => x.ResourceName.Contains("campaignBudgets", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ctx.Query.StatusCalls[0].Resources, x => x.ResourceName.Contains("campaignCriteria", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ctx.Publicacao.Historico, x => x.Operacao == "Ativacao" && x.RequestId == "req-activate");
        Assert.All(ctx.Publicacao.Recursos.Where(x => x.TipoRecurso is "Campaign" or "AdGroup" or "Keyword" or "ResponsiveSearchAd"), x => Assert.Equal("ENABLED", x.Status));
        Assert.All(ctx.Publicacao.Recursos.Where(x => x.TipoRecurso is "Budget" or "CampaignCriterion"), x => Assert.Equal("PAUSED", x.Status));
    }

    [Fact]
    public async Task Ativar_RetryIdempotenteQuandoJaEstaEnabled()
    {
        var ctx = Context();
        ctx.Query.RemoteCampaignStatus = "ENABLED";
        foreach (var resource in ctx.Publicacao.Recursos)
        {
            resource.Status = "ENABLED";
        }

        var service = Service(ctx);
        await service.AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None);
        await service.AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None);

        Assert.Empty(ctx.Query.StatusCalls);
        Assert.Single(ctx.Publicacao.Historico, x => x.Operacao == "Ativacao");
        Assert.Contains(ctx.Publicacao.Historico, x => x.Operacao == "Ativacao" && x.RequestId == "req-snapshot");
    }

    [Fact]
    public async Task Ativar_ErroGoogleAdsPermaneceSanitizado()
    {
        var ctx = Context();
        ctx.Query.ThrowDiagnostic = true;

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => Service(ctx).AtivarAsync(ctx.Publicacao.Id, Confirm(), CancellationToken.None));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-secret", serialized);
        Assert.DoesNotContain("developer-secret", serialized);
        Assert.DoesNotContain("refresh-secret", serialized);
    }

    private static GoogleAdsStatusActionRequest Confirm() => new(true);

    private static TestContext Context(bool allowActivation = true, string testCustomerId = "9890172254")
    {
        var conta = new GoogleAdsConta { Id = Guid.NewGuid(), CustomerId = "9890172254", Nome = "Conta", Ativa = true, Padrao = true };
        var publication = new GoogleAdsPublicacao
        {
            Id = Guid.NewGuid(),
            GoogleAdsContaId = conta.Id,
            GoogleAdsPlanoPublicacaoId = Guid.NewGuid(),
            CampanhaId = Guid.NewGuid(),
            CustomerId = "9890172254",
            PreviewHash = "HASH",
            PreviewVersao = 1,
            Status = StatusPublicacaoGoogleAds.Reconciliada,
            DataCriacao = DateTime.UtcNow,
            Recursos =
            [
                new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), TipoRecurso = "Campaign", ResourceName = "customers/9890172254/campaigns/1", Status = "PAUSED" },
                new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), TipoRecurso = "AdGroup", ResourceName = "customers/9890172254/adGroups/2", Status = "PAUSED" },
                new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), TipoRecurso = "Keyword", ResourceName = "customers/9890172254/adGroupCriteria/3", Status = "PAUSED" },
                new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), TipoRecurso = "Keyword", ResourceName = "customers/9890172254/adGroupCriteria/4", Status = "PAUSED" },
                new GoogleAdsRecursoPublicado { Id = Guid.NewGuid(), TipoRecurso = "ResponsiveSearchAd", ResourceName = "customers/9890172254/adGroupAds/5", Status = "PAUSED" }
            ]
        };
        foreach (var resource in publication.Recursos)
        {
            resource.GoogleAdsPublicacaoId = publication.Id;
        }

        return new TestContext(publication, conta, new Publications(publication), new Contas(conta), new Syncs(), new Query(), new Token(), new Resolver(allowActivation, testCustomerId));
    }

    private static GoogleAdsSynchronizationService Service(TestContext ctx) => new(ctx.Publications, ctx.Contas, ctx.Syncs, ctx.Query, ctx.Token, ctx.Resolver);

    private sealed record TestContext(GoogleAdsPublicacao Publicacao, GoogleAdsConta Conta, Publications Publications, Contas Contas, Syncs Syncs, Query Query, Token Token, Resolver Resolver);

    private sealed class Query : IGoogleAdsSynchronizationQueryClient
    {
        public string RemoteCampaignStatus { get; set; } = "PAUSED";
        public bool ThrowDiagnostic { get; set; }
        public List<StatusCall> StatusCalls { get; } = [];

        public Task<GoogleAdsRemoteStatusSnapshot> GetRemoteStatusAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GoogleAdsRemoteStatusSnapshot(resources.First(x => x.TipoRecurso == "Campaign").ResourceName, "Campaign", RemoteCampaignStatus, null, null, null, null, [], [], null, [], [], "req-snapshot"));
        }

        public Task SetCampaignStatusAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, string status, CancellationToken cancellationToken)
        {
            StatusCalls.Add(new StatusCall(status, [new("Campaign", campaignResourceName, null, null, "PAUSED")]));
            return Task.CompletedTask;
        }

        public Task<string?> SetResourceStatusesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, string status, CancellationToken cancellationToken)
        {
            if (ThrowDiagnostic)
            {
                throw new GoogleAdsDiagnosticException(new GoogleAdsDiagnosticResponse(false, "google_ads_error", "Bearer [redacted] developer_token=[redacted] refresh_token=[redacted]", "req-error", []));
            }

            StatusCalls.Add(new StatusCall(status, resources.ToArray()));
            return Task.FromResult<string?>("req-activate");
        }
    }

    private sealed record StatusCall(string Status, IReadOnlyList<GoogleAdsPublishedResourceDto> Resources);

    private sealed class Publications(GoogleAdsPublicacao publication) : IGoogleAdsPublicationRepository
    {
        public Task<GoogleAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == publication.Id ? publication : null);
        public Task<GoogleAdsPublicacao?> ObterPorPreviewVersaoHashAsync(Guid previewId, int versao, string hash, CancellationToken cancellationToken) => Task.FromResult<GoogleAdsPublicacao?>(null);
        public Task<IReadOnlyList<GoogleAdsPublicacao>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacao>>([publication]);
        public Task<IReadOnlyList<GoogleAdsPublicacao>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacao>>([publication]);
        public Task<IReadOnlyList<GoogleAdsPublicacaoHistorico>> ListarHistoricoAsync(Guid publicacaoId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacaoHistorico>>(publication.Historico.ToArray());
        public Task AdicionarAsync(GoogleAdsPublicacao publicacao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarRecursoAsync(GoogleAdsRecursoPublicado recurso, CancellationToken cancellationToken) { publication.Recursos.Add(recurso); return Task.CompletedTask; }
        public Task AdicionarHistoricoAsync(GoogleAdsPublicacaoHistorico historico, CancellationToken cancellationToken) { publication.Historico.Add(historico); return Task.CompletedTask; }
        public Task AdicionarOperacaoAsync(GoogleAdsOperacaoPublicacao operacao, CancellationToken cancellationToken) { publication.Operacoes.Add(operacao); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Contas(GoogleAdsConta conta) : IGoogleAdsContaRepository
    {
        public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == conta.Id ? conta : null);
        public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken) => Task.FromResult(customerId == conta.CustomerId ? conta : null);
        public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken) => Task.FromResult<GoogleAdsConta?>(conta);
        public Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsConta>>([conta]);
        public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Syncs : IGoogleAdsSynchronizationRepository
    {
        public List<GoogleAdsSincronizacao> Items { get; } = [];
        public Task AdicionarAsync(GoogleAdsSincronizacao sincronizacao, CancellationToken cancellationToken) { Items.Add(sincronizacao); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Resolver(bool allowActivation, string testCustomerId) : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "AllowTestAccountActivation" => allowActivation.ToString(),
                "EnableRealPublishing" => "true",
                "UseTestAccount" => "true",
                "TestCustomerId" => testCustomerId,
                "DeveloperToken" => "developer-secret",
                _ => string.Empty
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Token", StringComparison.OrdinalIgnoreCase)));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Token : IGoogleAdsTokenService
    {
        public Task<string> ObterAccessTokenValidoAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.FromResult("access-secret");
    }
}
