using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsPublishingTests
{
    [Fact]
    public async Task PreviewInvalidoBloqueia()
    {
        var ctx = Context();
        ctx.Preview.Status = StatusPlanoPublicacaoGoogleAds.Invalido;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).PrepararAsync(ctx.Preview.Id, CancellationToken.None));

        Assert.Contains("Preview precisa estar Valido", ex.Message);
    }

    [Fact]
    public async Task ValidacaoRemotaUsaValidateOnlyENaoCriaRecursos()
    {
        var ctx = Context();

        var result = await Service(ctx).ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);

        Assert.True(result.Valido);
        Assert.True(ctx.Mutation.LastValidateOnly);
        Assert.Empty(ctx.Publicacoes.Items.Single().Recursos);
    }

    [Fact]
    public async Task PublicacaoExigeValidacaoRemota()
    {
        var ctx = Context();
        var prepared = await Service(ctx).PrepararAsync(ctx.Preview.Id, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None));

        Assert.Contains("Validacao remota", ex.Message);
    }

    [Fact]
    public async Task PublicaComoPausedEPersisteResourceNames()
    {
        var ctx = Context();
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);
        var prepared = await service.PrepararAsync(ctx.Preview.Id, CancellationToken.None);

        var published = await service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None);

        Assert.Equal(StatusPublicacaoGoogleAds.Publicada, published.Status);
        Assert.All(ctx.Mutation.LastPlan!.Operations, op => Assert.Contains("PAUSED", op.PayloadJson));
        Assert.DoesNotContain(ctx.Mutation.LastPlan.Operations, op => op.PayloadJson.Contains("ENABLED", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(published.Recursos, x => x.TipoRecurso == "Campaign" && x.ResourceName.Contains("/campaigns/"));
    }

    [Fact]
    public async Task TokenConfirmacaoInvalidoOuFalseBloqueia()
    {
        var ctx = Context();
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest("x", false), CancellationToken.None));
    }

    [Fact]
    public async Task IdempotenciaRetornaPublicacaoExistente()
    {
        var ctx = Context();
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);
        var prepared = await service.PrepararAsync(ctx.Preview.Id, CancellationToken.None);
        var first = await service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None);

        var second = await service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(2, ctx.Mutation.MutateCalls);
    }

    [Fact]
    public async Task FalhaApiMarcaFalhou()
    {
        var ctx = Context();
        ctx.Mutation.PublishSuccess = false;
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);
        var prepared = await service.PrepararAsync(ctx.Preview.Id, CancellationToken.None);

        var result = await service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None);

        Assert.Equal(StatusPublicacaoGoogleAds.Falhou, result.Status);
        Assert.NotEmpty(result.Erros);
    }

    [Fact]
    public async Task ContaTesteBloqueiaCustomerProducao()
    {
        var ctx = Context(useTest: true);
        ctx.Conta.CustomerId = "9999999999";

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).PrepararAsync(ctx.Preview.Id, CancellationToken.None));

        Assert.Contains("Modo teste", ex.Message);
    }

    [Fact]
    public async Task ReconciliacaoNaoCriaRecursos()
    {
        var ctx = Context();
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);
        var prepared = await service.PrepararAsync(ctx.Preview.Id, CancellationToken.None);
        var published = await service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None);
        var mutateCallsBeforeReconciliation = ctx.Mutation.MutateCalls;

        var reconciled = await service.ReconciliarAsync(published.Id, CancellationToken.None);

        Assert.Equal(StatusPublicacaoGoogleAds.Reconciliada, reconciled.Status);
        Assert.Equal(published.Recursos.Count, reconciled.Recursos.Count);
        Assert.Equal(mutateCallsBeforeReconciliation, ctx.Mutation.MutateCalls);
        Assert.Equal(published.Recursos.Count, ctx.Query.LastResources.Count);
        Assert.All(reconciled.Recursos, x => Assert.Equal("PAUSED", x.Status));
    }

    [Fact]
    public async Task DryRunNaoChamaGoogleENaoCriaPublicacao()
    {
        var ctx = Context();

        var dryRun = await Service(ctx).DryRunAsync(ctx.Preview.Id, CancellationToken.None);

        Assert.True(dryRun.Valido);
        Assert.Equal(4, dryRun.QuantidadeOperacoes);
        Assert.Equal(0, ctx.Mutation.MutateCalls);
        Assert.Empty(ctx.Publicacoes.Items);
    }

    [Fact]
    public async Task FeatureFlagDesligadaBloqueiaPublicacaoReal()
    {
        var ctx = Context(enableRealPublishing: false);
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);
        var prepared = await service.PrepararAsync(ctx.Preview.Id, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PublicarAsync(ctx.Preview.Id, new GoogleAdsPublishRequest(prepared.ConfirmationToken, true), CancellationToken.None));

        Assert.Contains("desabilitada", ex.Message);
    }

    [Fact]
    public async Task HistoricoRegistraTransicoes()
    {
        var ctx = Context();
        var service = Service(ctx);
        await service.ValidarRemotamenteAsync(ctx.Preview.Id, CancellationToken.None);

        var history = await service.HistoricoAsync(ctx.Publicacoes.Items.Single().Id, CancellationToken.None);

        Assert.Contains(history, x => x.StatusNovo == StatusPublicacaoGoogleAds.Validada);
    }

    private static TestContext Context(bool useTest = true, bool enableRealPublishing = true)
    {
        var campanha = new Campanha { Id = Guid.NewGuid(), Status = StatusCampanha.Revisada, Publicada = true, Ativo = true };
        var conta = new GoogleAdsConta { Id = Guid.NewGuid(), CustomerId = "1234567890", Nome = "Conta", Ativa = true, Padrao = true, AccessTokenProtegido = "protected:access", RefreshTokenProtegido = "protected:refresh", AccessTokenExpiraEm = DateTime.UtcNow.AddHours(1) };
        var payload = new GoogleAdsPreviewPayload(
            new GoogleAdsCampaignPlan("Campanha", "SEARCH", "ENABLED", "SEARCH", true, false, false, "Leads", "BRL", "pt", "BR", "https://leadengine.test/lp/x"),
            new GoogleAdsBudgetPlan("Budget", 10, 10_000_000, "STANDARD", false),
            [new GoogleAdsAdGroupPlan("Grupo", "ENABLED", null, null, [new("plano saude", "PHRASE", "PAUSED", "Campanha")], [new("emprego", "PHRASE", "Campanha")], new(["Titulo Um", "Titulo Dois", "Titulo Tres"], ["Descricao um", "Descricao dois"], ["https://leadengine.test/lp/x"], "plano", "saude", "ENABLED"))]);
        var preview = new GoogleAdsPlanoPublicacao { Id = Guid.NewGuid(), CampanhaId = campanha.Id, GoogleAdsContaId = conta.Id, NomeCampanha = "Campanha", Status = StatusPlanoPublicacaoGoogleAds.Valido, OrcamentoDiario = 10, CodigoMoeda = "BRL", Idioma = "pt", Pais = "BR", UrlFinal = "https://leadengine.test/lp/x", Versao = 1, ConteudoHash = "HASH", PayloadPreviewJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)), DataCriacao = DateTime.UtcNow };
        return new TestContext(campanha, conta, preview, new Campanhas(campanha), new Contas(conta), new Previews(preview), new Publicacoes(), new Mutation(), new Query(), new Resolver(useTest, enableRealPublishing));
    }

    private static GoogleAdsPublishingService Service(TestContext ctx)
    {
        return new GoogleAdsPublishingService(ctx.Previews, ctx.Campanhas, ctx.Contas, ctx.Publicacoes, new Builder(), ctx.Mutation, ctx.Query, new Token(), ctx.Resolver);
    }

    private sealed record TestContext(Campanha Campanha, GoogleAdsConta Conta, GoogleAdsPlanoPublicacao Preview, Campanhas Campanhas, Contas Contas, Previews Previews, Publicacoes Publicacoes, Mutation Mutation, Query Query, Resolver Resolver);

    private sealed class Builder : IGoogleAdsOperationBuilder
    {
        public Task<GoogleAdsOperationPlan> BuildAsync(GoogleAdsPlanoPublicacao preview, string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GoogleAdsOperationPlan(preview.ConteudoHash, preview.Versao, customerId, "geoTargetConstants/2076", "languageConstants/1014",
            [
                new("Budget", "Budget", "Budget", "{\"status\":\"PAUSED\"}"),
                new("Campaign", "Campaign", "Campaign", "{\"status\":\"PAUSED\"}"),
                new("AdGroup", "AdGroup", "AdGroup", "{\"status\":\"PAUSED\"}"),
                new("ResponsiveSearchAd", "Ad", "Ad", "{\"status\":\"PAUSED\"}")
            ], []));
        }
    }

    private sealed class Mutation : IGoogleAdsMutationClient
    {
        public bool LastValidateOnly { get; private set; }
        public int MutateCalls { get; private set; }
        public bool PublishSuccess { get; set; } = true;
        public GoogleAdsOperationPlan? LastPlan { get; private set; }
        public Task<GoogleAdsMutationResult> MutateAsync(string customerId, string accessToken, string developerToken, GoogleAdsOperationPlan plan, bool validateOnly, CancellationToken cancellationToken)
        {
            LastValidateOnly = validateOnly;
            LastPlan = plan;
            MutateCalls++;
            if (validateOnly) return Task.FromResult(new GoogleAdsMutationResult(true, "req-val", [], [], false));
            if (!PublishSuccess) return Task.FromResult(new GoogleAdsMutationResult(false, "req-pub", [], [new("invalid_budget", "Budget invalido", "Budget", 0, "amount", "0", "req-pub", false, "Ajuste o budget")], false));
            return Task.FromResult(new GoogleAdsMutationResult(true, "req-pub", [
                new("Budget", $"customers/{customerId}/campaignBudgets/1", "1", "Budget", "PAUSED"),
                new("Campaign", $"customers/{customerId}/campaigns/2", "2", "Campaign", "PAUSED")
            ], [], false));
        }
        public Task<IReadOnlyList<GoogleAdsPublishedResourceDto>> CheckResourcesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken) => Task.FromResult(resources);
    }

    private sealed class Query : IGoogleAdsResourceQueryClient
    {
        public IReadOnlyList<GoogleAdsPublishedResourceDto> LastResources { get; private set; } = [];

        public Task<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>> CheckResourcesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
        {
            LastResources = resources;
            return Task.FromResult<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>>(resources.Select(x => new GoogleAdsPublishedResourceCheckDto(x.TipoRecurso, x.ResourceName, x.ExternalId, x.Nome, x.Status, true, false, "ok")).ToArray());
        }
    }

    private sealed class Resolver(bool useTest, bool enableRealPublishing) : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch { "DeveloperToken" => "dev", "UseTestAccount" => useTest.ToString(), "EnableRealPublishing" => enableRealPublishing.ToString(), "TestCustomerId" => "1234567890", _ => "" };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Token")));
        }
        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Token : IGoogleAdsTokenService { public Task<string> ObterAccessTokenValidoAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.FromResult("access"); }
    private sealed class Campanhas(Campanha campanha) : ICampanhaRepository
    {
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) => Task.CompletedTask; public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask; public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(false); public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == campanha.Id ? campanha : null); public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<Campanha?>(null); public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]); public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>([campanha]); public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class Contas(GoogleAdsConta conta) : IGoogleAdsContaRepository
    {
        public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == conta.Id ? conta : null); public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken) => Task.FromResult(customerId == conta.CustomerId ? conta : null); public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken) => Task.FromResult<GoogleAdsConta?>(conta); public Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsConta>>([conta]); public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.CompletedTask; public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class Previews(GoogleAdsPlanoPublicacao preview) : IGoogleAdsPlanoPublicacaoRepository
    {
        public Task<GoogleAdsPlanoPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == preview.Id ? preview : null); public Task<GoogleAdsPlanoPublicacao?> ObterPorCampanhaIdAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult(campanhaId == preview.CampanhaId ? preview : null); public Task AdicionarAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken) => Task.CompletedTask; public Task RemoverAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken) => Task.CompletedTask; public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class Publicacoes : IGoogleAdsPublicationRepository
    {
        public List<GoogleAdsPublicacao> Items { get; } = [];
        public Task<GoogleAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<GoogleAdsPublicacao?> ObterPorPreviewVersaoHashAsync(Guid previewId, int versao, string hash, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.GoogleAdsPlanoPublicacaoId == previewId && x.PreviewVersao == versao && x.PreviewHash == hash));
        public Task<IReadOnlyList<GoogleAdsPublicacao>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacao>>(Items.Where(x => x.CampanhaId == campanhaId).ToArray());
        public Task<IReadOnlyList<GoogleAdsPublicacao>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacao>>(Items);
        public Task<IReadOnlyList<GoogleAdsPublicacaoHistorico>> ListarHistoricoAsync(Guid publicacaoId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsPublicacaoHistorico>>(Items.Single(x => x.Id == publicacaoId).Historico.ToArray());
        public Task AdicionarAsync(GoogleAdsPublicacao publicacao, CancellationToken cancellationToken) { Items.Add(publicacao); return Task.CompletedTask; }
        public Task AdicionarRecursoAsync(GoogleAdsRecursoPublicado recurso, CancellationToken cancellationToken) { Items.Single(x => x.Id == recurso.GoogleAdsPublicacaoId).Recursos.Add(recurso); return Task.CompletedTask; }
        public Task AdicionarHistoricoAsync(GoogleAdsPublicacaoHistorico historico, CancellationToken cancellationToken) { Items.Single(x => x.Id == historico.GoogleAdsPublicacaoId).Historico.Add(historico); return Task.CompletedTask; }
        public Task AdicionarOperacaoAsync(GoogleAdsOperacaoPublicacao operacao, CancellationToken cancellationToken) { Items.Single(x => x.Id == operacao.GoogleAdsPublicacaoId).Operacoes.Add(operacao); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
