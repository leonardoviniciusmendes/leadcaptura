using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.GoogleAds;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsPreviewTests
{
    [Fact]
    public async Task CampanhaAprovadaGeraPreviewValido()
    {
        var ctx = Context();
        var service = Service(ctx);

        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal(StatusPlanoPublicacaoGoogleAds.Valido, preview.Status);
        Assert.Equal("SEARCH", preview.Payload.Campaign.AdvertisingChannelType);
        Assert.Equal("PAUSED", preview.Payload.Campaign.Status);
        Assert.False(preview.Payload.Campaign.IncludeDisplayNetwork);
        Assert.Equal(10_000_000, preview.OrcamentoMicros);
        Assert.DoesNotContain("token", System.Text.Json.JsonSerializer.Serialize(preview.Payload), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CampanhaNaoAprovadaFalha()
    {
        var ctx = Context();
        ctx.Campanha.Status = StatusCampanha.Gerada;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None));

        Assert.Contains("aprovada", ex.Message);
    }

    [Fact]
    public async Task LandingNaoPublicadaFalha()
    {
        var ctx = Context();
        ctx.Campanha.Publicada = false;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None));

        Assert.Contains("Landing", ex.Message);
    }

    [Fact]
    public async Task ContaPadraoAusenteFalha()
    {
        var ctx = Context();
        ctx.Contas.Items.Clear();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None));

        Assert.Contains("conta Google Ads", ex.Message);
    }

    [Fact]
    public void ConversaoDecimalParaMicros()
    {
        Assert.Equal(10_000_000, GoogleAdsMoney.ToMicros(10m));
        Assert.Equal(1_235_000, GoogleAdsMoney.ToMicros(1.235m));
    }

    [Fact]
    public async Task HeadlineEDescriptionForaDoLimiteInvalidam()
    {
        var ctx = Context();
        ctx.Campanha.TitulosAnunciosJson = Json(["Titulo valido", "Outro titulo", "Este titulo tem muito mais de trinta caracteres"]);
        ctx.Campanha.DescricoesAnunciosJson = Json(["Descricao valida", new string('x', 91)]);

        var preview = await Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal(StatusPlanoPublicacaoGoogleAds.Invalido, preview.Status);
        Assert.Contains(preview.Erros, x => x.Contains("Headline"));
        Assert.Contains(preview.Erros, x => x.Contains("Description"));
    }

    [Fact]
    public async Task DeduplicaKeywordsENegativas()
    {
        var ctx = Context();
        ctx.Campanha.PalavrasChaveJson = Json(["Plano Saude", "plano saúde", "cotacao plano"]);
        ctx.Campanha.PalavrasChaveNegativasJson = Json(["boleto", "Boleto"]);

        var preview = await Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal(2, preview.Payload.AdGroups[0].Keywords.Count);
        Assert.Single(preview.Payload.AdGroups[0].NegativeKeywords);
        Assert.Contains(preview.Payload.AdGroups[0].Keywords, x => x.MatchType == "EXACT");
    }

    [Fact]
    public async Task GeraPathsDoSlug()
    {
        var ctx = Context();
        ctx.Campanha.Slug = "plano-saude-empresarial-rj";

        var preview = await Service(ctx).GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        var rsa = preview.Payload.AdGroups[0].ResponsiveSearchAd;
        Assert.Equal("plano-saude", rsa.Path1);
        Assert.Equal("empresarial", rsa.Path2);
    }

    [Theory]
    [InlineData("http://localhost:5173", "http://localhost:5173/lp/plano-familiar-copacabana")]
    [InlineData("http://localhost:5173/", "http://localhost:5173/lp/plano-familiar-copacabana")]
    [InlineData("http://localhost:5173/leadcaptura", "http://localhost:5173/leadcaptura/lp/plano-familiar-copacabana")]
    [InlineData("http://localhost:5173/leadcaptura/", "http://localhost:5173/leadcaptura/lp/plano-familiar-copacabana")]
    [InlineData("https://leadengine.example.com", "https://leadengine.example.com/lp/plano-familiar-copacabana")]
    public void UrlPublica_NormalizaBaseSlugESubpath(string publicBaseUrl, string expected)
    {
        var result = CampaignPublicUrlBuilder.Build("plano-familiar-copacabana", publicBaseUrl);

        Assert.True(result.Valida);
        Assert.Equal(expected, result.Url);
    }

    [Fact]
    public void UrlPublica_InfereSubpathAPartirDoRedirectUri()
    {
        var result = CampaignPublicUrlBuilder.InferBaseUrlFromRedirectUri("http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1");

        Assert.Equal("http://localhost:5173/leadcaptura", result);
    }

    [Fact]
    public async Task Preview_UsaApplicationPublicBaseUrlComSubpath()
    {
        var ctx = Context();
        ctx.Campanha.Slug = "plano-familiar-copacabana";
        ctx.Campanha.UrlPublica = "/lp/plano-familiar-copacabana";
        var resolver = new Resolver("http://localhost:5173/leadcaptura");
        var service = Service(ctx, resolver);

        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal("http://localhost:5173/leadcaptura/lp/plano-familiar-copacabana", preview.UrlFinal);
        Assert.Equal(StatusPlanoPublicacaoGoogleAds.Valido, preview.Status);
    }

    [Fact]
    public async Task Preview_InferePublicBaseUrlDoRedirectUriQuandoApplicationPublicBaseUrlVazio()
    {
        var ctx = Context();
        ctx.Campanha.Slug = "plano-familiar-copacabana";
        ctx.Campanha.UrlPublica = "/lp/plano-familiar-copacabana";
        var resolver = new Resolver(publicBaseUrl: "", redirectUri: "http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1");
        var service = Service(ctx, resolver);

        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal("http://localhost:5173/leadcaptura/lp/plano-familiar-copacabana", preview.UrlFinal);
        Assert.Equal(StatusPlanoPublicacaoGoogleAds.Valido, preview.Status);
    }

    [Fact]
    public async Task RegenerarIncrementaVersao()
    {
        var ctx = Context();
        var service = Service(ctx);
        var first = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        ctx.Campanha.OrcamentoDiario = 20;

        var second = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(2, second.Versao);
    }

    [Fact]
    public async Task DetectaDesatualizacaoPorHash()
    {
        var ctx = Context();
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        ctx.Campanha.Slug = "slug-alterado";

        var loaded = await service.ObterAsync(preview.Id, CancellationToken.None);

        Assert.True(loaded.Desatualizado);
        Assert.Equal(StatusPlanoPublicacaoGoogleAds.Desatualizado, loaded.Status);
    }

    [Fact]
    public async Task SugestaoNaoAplicaAutomaticamenteEAplicacaoAtualizaPreview()
    {
        var ctx = Context();
        ctx.Campanha.TitulosAnunciosJson = Json(["Titulo valido", "Outro titulo", "Titulo muito longo acima de trinta caracteres"]);
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        var sugestoes = await service.SugerirAjustesAsync(preview.Id, new GoogleAdsSugerirAjustesRequest(["headlines"]), CancellationToken.None);
        var stillOriginal = await service.ObterAsync(preview.Id, CancellationToken.None);
        var updated = await service.AplicarSugestaoAsync(preview.Id, new AplicarGoogleAdsSugestaoRequest("headlines", 2, sugestoes.Sugestoes[0].Sugestao), CancellationToken.None);

        Assert.Contains("acima de trinta", stillOriginal.Payload.AdGroups[0].ResponsiveSearchAd.Headlines[2]);
        Assert.Equal("Titulo ajustado", updated.Payload.AdGroups[0].ResponsiveSearchAd.Headlines[2]);
    }

    [Fact]
    public async Task AtualizacaoDoPreviewNaoAlteraCampanhaOriginalEExclui()
    {
        var ctx = Context();
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        await service.AtualizarAsync(preview.Id, new AtualizarGoogleAdsPreviewRequest("Nome tecnico", 15, null, null, null, null, null, null, null, null), CancellationToken.None);
        await service.ExcluirAsync(preview.Id, CancellationToken.None);

        Assert.Equal("Campanha teste", ctx.Campanha.Nome);
        Assert.Empty(ctx.Planos.Items);
    }

    [Fact]
    public async Task EditarCpcParaTresPersisteAposReloadERegeneracao()
    {
        var ctx = Context();
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Null(preview.Payload.AdGroups[0].CpcBid);
        Assert.Contains("CPC inicial nao configurado.", preview.Avisos);

        await service.AtualizarAsync(preview.Id, new AtualizarGoogleAdsPreviewRequest(null, null, null, 3m, null, null, null, null, null, null), CancellationToken.None);
        var reloaded = await service.ObterAsync(preview.Id, CancellationToken.None);
        var regenerated = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);

        Assert.Equal(3m, reloaded.Payload.AdGroups[0].CpcBid);
        Assert.Equal(3_000_000, reloaded.Payload.AdGroups[0].CpcBidMicros);
        Assert.DoesNotContain("CPC inicial nao configurado.", reloaded.Avisos);
        Assert.Equal(3m, regenerated.Payload.AdGroups[0].CpcBid);
        Assert.Equal(3_000_000, regenerated.Payload.AdGroups[0].CpcBidMicros);
        Assert.DoesNotContain("CPC inicial nao configurado.", regenerated.Avisos);
    }

    [Fact]
    public async Task LocalizacaoRioDeJaneiroPersisteEGeraCampaignCriterionSemFallbackBrasil()
    {
        var ctx = Context();
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        await service.AtualizarAsync(preview.Id, new AtualizarGoogleAdsPreviewRequest(null, null, null, 3m, null, null, null, null, null, null), CancellationToken.None);
        var reloaded = await service.ObterAsync(preview.Id, CancellationToken.None);

        var plan = await new GoogleAdsOperationBuilder(new GoogleAdsGeoTargetResolver(), new GoogleAdsLanguageResolver())
            .BuildAsync(ctx.Planos.Items.Single(), "1234567890", CancellationToken.None);

        Assert.Equal("BR", reloaded.Payload.Campaign.CountryCode);
        Assert.Equal("Rio de Janeiro, State of Rio de Janeiro, Brazil", reloaded.Payload.Campaign.LocationName);
        Assert.Equal("geoTargetConstants/1001655", reloaded.Payload.Campaign.GeoTargetResourceName);
        Assert.Equal("geoTargetConstants/1001655", plan.GeoTargetResourceName);
        Assert.NotEqual("geoTargetConstants/2076", plan.GeoTargetResourceName);
        Assert.Contains(plan.Operations, x => x.TipoRecurso == "CampaignCriterion" && x.PayloadJson.Contains("\"geoTargetResourceName\":\"geoTargetConstants/1001655\""));
        Assert.Contains(plan.Operations, x => x.TipoRecurso == "AdGroup" && x.PayloadJson.Contains("\"cpcBidMicros\":3000000"));
        Assert.All(plan.Operations, x => Assert.DoesNotContain("ENABLED", x.PayloadJson, StringComparison.OrdinalIgnoreCase));
        var operations = new GoogleAdsTypedOperationFactory().Create(plan);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.CampaignStatusEnum.Types.CampaignStatus.Paused, operations[1].CampaignOperation.Create.Status);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupStatusEnum.Types.AdGroupStatus.Paused, operations.Single(x => x.AdGroupOperation is not null).AdGroupOperation.Create.Status);
        Assert.All(operations.Where(x => x.AdGroupCriterionOperation is not null), x => Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupCriterionStatusEnum.Types.AdGroupCriterionStatus.Paused, x.AdGroupCriterionOperation.Create.Status));
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupAdStatusEnum.Types.AdGroupAdStatus.Paused, operations.Single(x => x.AdGroupAdOperation is not null).AdGroupAdOperation.Create.Status);
    }

    [Fact]
    public async Task SalvarPreviewLegadoPreencheLocalizacaoEstruturadaRioDeJaneiro()
    {
        var ctx = Context();
        ctx.Campanha.Id = Guid.Parse("01281be7-066d-4d92-98c2-55564d8cb18b");
        ctx.Campanha.Nome = "Captacao Plano Familiar Zona Sul RJ";
        ctx.Campanha.Regiao = "Zona Sul";
        ctx.Campanha.Cidade = "Rio de Janeiro";
        ctx.Campanha.Estado = "RJ";
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        var legacyPayload = preview.Payload with
        {
            Campaign = preview.Payload.Campaign with
            {
                LocationName = null,
                GeoTargetResourceName = null
            }
        };
        ctx.Planos.Items.Single().PayloadPreviewJson = System.Text.Json.JsonSerializer.Serialize(legacyPayload, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        await service.AtualizarAsync(preview.Id, new AtualizarGoogleAdsPreviewRequest(null, null, null, null, null, null, null, null, null, null), CancellationToken.None);
        var reloaded = await service.ObterAsync(preview.Id, CancellationToken.None);
        var plan = await new GoogleAdsOperationBuilder(new GoogleAdsGeoTargetResolver(), new GoogleAdsLanguageResolver())
            .BuildAsync(ctx.Planos.Items.Single(), "1234567890", CancellationToken.None);

        Assert.NotNull(reloaded.Payload.Campaign.LocationName);
        Assert.Equal("Rio de Janeiro, State of Rio de Janeiro, Brazil", reloaded.Payload.Campaign.LocationName);
        Assert.Equal("geoTargetConstants/1001655", reloaded.Payload.Campaign.GeoTargetResourceName);
        Assert.Equal("geoTargetConstants/1001655", plan.GeoTargetResourceName);
        Assert.NotEqual("geoTargetConstants/2076", plan.GeoTargetResourceName);
        Assert.Contains(plan.Operations, x => x.TipoRecurso == "CampaignCriterion" && x.PayloadJson.Contains("\"geoTargetResourceName\":\"geoTargetConstants/1001655\""));
        Assert.DoesNotContain(plan.Operations, x => x.TipoRecurso == "CampaignCriterion" && x.PayloadJson.Contains("\"geoTargetResourceName\":\"geoTargetConstants/2076\""));
    }

    [Fact]
    public async Task NomeEditadoDaCampaignPersisteEAlimentaOperationPlan()
    {
        var ctx = Context();
        ctx.Campanha.Nome = "Captação Plano Familiar Zona Sul RJ";
        var service = Service(ctx);
        var preview = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        var nomeEditado = "Captação Plano Familiar Zona Sul RJ - V2";

        await service.AtualizarAsync(preview.Id, new AtualizarGoogleAdsPreviewRequest(nomeEditado, null, null, null, null, null, null, null, null, null), CancellationToken.None);
        var reloaded = await service.ObterAsync(preview.Id, CancellationToken.None);
        var regenerated = await service.GerarOuAtualizarAsync(ctx.Campanha.Id, CancellationToken.None);
        var plan = await new GoogleAdsOperationBuilder(new GoogleAdsGeoTargetResolver(), new GoogleAdsLanguageResolver())
            .BuildAsync(ctx.Planos.Items.Single(), "1234567890", CancellationToken.None);
        var typedOperations = new GoogleAdsTypedOperationFactory().Create(plan);
        using var campaignOperationPayload = System.Text.Json.JsonDocument.Parse(plan.Operations.Single(x => x.TipoRecurso == "Campaign").PayloadJson);

        Assert.Equal(nomeEditado, reloaded.Payload.Campaign.Name);
        Assert.Equal(nomeEditado, reloaded.NomeCampanha);
        Assert.Equal(nomeEditado, regenerated.Payload.Campaign.Name);
        Assert.Equal(nomeEditado, campaignOperationPayload.RootElement.GetProperty("name").GetString());
        Assert.Equal(nomeEditado, typedOperations.Single(x => x.CampaignOperation is not null).CampaignOperation.Create.Name);
        Assert.NotEqual("Captação Plano Familiar Zona Sul RJ", campaignOperationPayload.RootElement.GetProperty("name").GetString());
    }

    private static TestContext Context()
    {
        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = "Campanha teste",
            Status = StatusCampanha.Revisada,
            Publicada = true,
            Ativo = true,
            Slug = "plano-saude-empresarial-rj",
            UrlPublica = "https://leadengine.test/lp/plano-saude-empresarial-rj",
            OrcamentoDiario = 10,
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            TipoPublico = TipoPublicoCampanha.Empresa,
            Operadora = "Amil",
            BeneficiosJson = Json(["Atendimento consultivo", "Compare planos", "Cotacao por perfil"]),
            PalavrasChaveJson = Json(["plano de saude empresarial", "cotacao plano saude", "amil empresa"]),
            PalavrasChaveNegativasJson = Json(["boleto", "emprego"]),
            TitulosAnunciosJson = Json(["Plano Saude RJ", "Cotacao Familiar", "Fale no WhatsApp"]),
            DescricoesAnunciosJson = Json(["Compare opcoes conforme seu perfil.", "Atendimento consultivo para planos."])
        };
        var campanhas = new Campanhas(campanha);
        var contas = new Contas();
        contas.Items.Add(new GoogleAdsConta { Id = Guid.NewGuid(), CustomerId = "1234567890", Nome = "Conta teste", Ativa = true, Padrao = true, DataConexao = DateTime.UtcNow });
        return new TestContext(campanha, campanhas, contas, new Planos());
    }

    private static GoogleAdsPreviewService Service(TestContext ctx, Resolver? resolver = null)
    {
        resolver ??= new Resolver();
        return new GoogleAdsPreviewService(
            ctx.Campanhas,
            ctx.Contas,
            ctx.Planos,
            new GoogleAdsCampaignMappingService(resolver),
            new GoogleAdsValidationService(),
            new Copy(),
            resolver);
    }

    private static string Json(IEnumerable<string> value) => System.Text.Json.JsonSerializer.Serialize(value);

    private sealed record TestContext(Campanha Campanha, Campanhas Campanhas, Contas Contas, Planos Planos);

    private sealed class Resolver(string publicBaseUrl = "https://leadengine.test", string redirectUri = "http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1") : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "ClientId" => "client-id",
                "ClientSecret" => "secret",
                "DeveloperToken" => "dev-token",
                "DefaultDailyBudget" => "10.00",
                "DefaultCountryCode" => "BR",
                "DefaultLanguageCode" => "pt",
                "DefaultCurrencyCode" => "BRL",
                "DefaultKeywordMatchType" => "Phrase",
                "DefaultCampaignStatus" => "PAUSED",
                "EnableBroadMatch" => "false",
                "PublicBaseUrl" => publicBaseUrl,
                "RedirectUri" => redirectUri,
                _ => ""
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Secret") || chave.Contains("Token")));
        }
        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Copy : IGoogleAdsCopyAdjustmentService
    {
        public Task<IReadOnlyList<GoogleAdsCopySuggestionItem>> SugerirAsync(GoogleAdsPreviewPayload payload, IReadOnlyList<string> campos, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsCopySuggestionItem>>([new("headlines", 2, payload.AdGroups[0].ResponsiveSearchAd.Headlines[2], "Titulo ajustado", 30)]);
        }
    }

    private sealed class Campanhas(Campanha campanha) : ICampanhaRepository
    {
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == campanha.Id ? campanha : null);
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<Campanha?>(null);
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]);
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>([campanha]);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Contas : IGoogleAdsContaRepository
    {
        public List<GoogleAdsConta> Items { get; } = [];
        public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.CustomerId == customerId));
        public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Padrao && x.Ativa));
        public Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsConta>>(Items);
        public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken) { Items.Add(conta); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Planos : IGoogleAdsPlanoPublicacaoRepository
    {
        public List<GoogleAdsPlanoPublicacao> Items { get; } = [];
        public Task<GoogleAdsPlanoPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<GoogleAdsPlanoPublicacao?> ObterPorCampanhaIdAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.CampanhaId == campanhaId));
        public Task AdicionarAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken) { Items.Add(plano); return Task.CompletedTask; }
        public Task RemoverAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken) { Items.Remove(plano); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
