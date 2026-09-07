using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class CreativeQualityGateServiceTests
{
    [Fact]
    public async Task Gate_SemanticMismatchTrueScore18_Blocked()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: true, score: 18);

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("BLOCKED", gate.Status);
        Assert.False(gate.CanApprove);
        Assert.True(gate.RequiresOverride);
        Assert.True(gate.SemanticMismatch);
        Assert.Equal(18, gate.Score);
    }

    [Fact]
    public async Task Gate_SemanticMismatchFalseScore30_Blocked()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: false, score: 30);

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("BLOCKED", gate.Status);
        Assert.False(gate.CanApprove);
        Assert.True(gate.RequiresOverride);
        Assert.False(gate.SemanticMismatch);
    }

    [Fact]
    public async Task Gate_SemanticMismatchFalseScore55_Warning()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: false, score: 55);

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("WARNING", gate.Status);
        Assert.True(gate.CanApprove);
        Assert.False(gate.RequiresOverride);
    }

    [Fact]
    public async Task Gate_SemanticMismatchFalseScore85_Approved()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: false, score: 85);

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("APPROVED", gate.Status);
        Assert.True(gate.CanApprove);
    }

    [Fact]
    public async Task Gate_RawPersistidoEmEscalaZeroADezPositiva_NormalizaEAprova()
    {
        var ctx = TestContext.Create();
        var asset = ctx.AddAsset(selected: true, semanticMismatch: false, score: 9);
        var analysis = asset.Analyses.Single();
        analysis.VisualQualityScore = 9;
        analysis.BrandFitScore = 10;
        analysis.TextDensityScore = 9;
        analysis.RawResponseJson = JsonSerializer.Serialize(new
        {
            summary = "Excelente video que alinha perfeitamente com o briefing.",
            detectedText = "ESTETICA COM CRITERIO",
            visualQualityScore = 9,
            campaignFitScore = 10,
            brandFitScore = 10,
            textDensityScore = 9,
            messageConsistencyScore = 10,
            semanticMismatch = false,
            placementRecommendations = new
            {
                facebookFeed = "recommended",
                instagramFeed = "recommended",
                stories = "recommended",
                reels = "recommended"
            }
        });

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("APPROVED", gate.Status);
        Assert.False(gate.SemanticMismatch);
        Assert.True(gate.Score >= 90);
    }

    [Fact]
    public async Task Gate_CreativeAssetSemAnalise_NotAnalyzed()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, withAnalysis: false);

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("NOT_ANALYZED", gate.Status);
        Assert.True(gate.CanApprove);
        Assert.Null(gate.Score);
    }

    [Fact]
    public async Task Gate_CampanhaSemCreativeAsset_NoCreativePreservaCompatibilidade()
    {
        var ctx = TestContext.Create();

        var gate = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal("NO_CREATIVE", gate.Status);
        Assert.True(gate.CanApprove);
        Assert.False(gate.RequiresOverride);
    }

    [Fact]
    public async Task Aprovacao_BlockedSemOverride_NaoAprova()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: true, score: 18);

        await Assert.ThrowsAsync<CreativeQualityGateException>(() =>
            ctx.Review().AprovarCampanhaAsync(ctx.Campaign.Id, new AprovarCampanhaRequest(), CancellationToken.None));

        Assert.Equal(StatusCampanha.Gerada, ctx.Campaign.Status);
        Assert.Empty(ctx.Overrides.Items);
    }

    [Fact]
    public async Task Aprovacao_BlockedOverrideMotivoVazio_Rejeita()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: true, score: 18);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            ctx.Review().AprovarCampanhaAsync(ctx.Campaign.Id, new AprovarCampanhaRequest(true, " "), CancellationToken.None));

        Assert.Equal(StatusCampanha.Gerada, ctx.Campaign.Status);
        Assert.Empty(ctx.Overrides.Items);
    }

    [Fact]
    public async Task Aprovacao_BlockedOverrideMotivoValido_AprovaEAudita()
    {
        var ctx = TestContext.Create();
        var asset = ctx.AddAsset(selected: true, semanticMismatch: true, score: 18);
        var analysis = asset.Analyses.Single();

        var result = await ctx.Review().AprovarCampanhaAsync(ctx.Campaign.Id, new AprovarCampanhaRequest(true, "Cliente aprovou manualmente."), CancellationToken.None);

        Assert.Equal(StatusCampanha.Revisada, result.Campanha.Status);
        var audit = Assert.Single(ctx.Overrides.Items);
        Assert.Equal(ctx.Campaign.Id, audit.CampaignId);
        Assert.Equal(asset.Id, audit.CreativeAssetId);
        Assert.Equal(analysis.Id, audit.CreativeAssetAnalysisId);
        Assert.Equal(18, audit.RankingScore);
        Assert.True(audit.SemanticMismatch);
        Assert.Equal("Cliente aprovou manualmente.", audit.Reason);
        Assert.Equal("admin@leadengine.test", audit.User);
    }

    [Fact]
    public async Task Gate_TrocaCreativeAsset_UsaAnaliseDaNovaImagem()
    {
        var ctx = TestContext.Create();
        var oldAsset = ctx.AddAsset(selected: true, semanticMismatch: true, score: 18);
        var newAsset = ctx.AddAsset(selected: false, semanticMismatch: false, score: 85);

        var before = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);
        oldAsset.IsSelected = false;
        newAsset.IsSelected = true;
        var after = await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal(oldAsset.Id, before.CreativeAssetId);
        Assert.Equal("BLOCKED", before.Status);
        Assert.Equal(newAsset.Id, after.CreativeAssetId);
        Assert.Equal("APPROVED", after.Status);
    }

    [Fact]
    public async Task Gate_NaoChamaOpenRouter()
    {
        var ctx = TestContext.Create();
        ctx.AddAsset(selected: true, semanticMismatch: false, score: 85);

        await ctx.Gate().EvaluateAsync(ctx.Campaign.Id, CancellationToken.None);

        Assert.Equal(0, ctx.Assets.AnalysisProviderCalls);
    }

    private sealed class TestContext
    {
        public Campanha Campaign { get; } = ValidCampaign();
        public Campaigns Campaigns { get; }
        public CreativeAssets Assets { get; } = new();
        public Overrides Overrides { get; } = new();
        public RequestContext RequestContext { get; } = new();

        private TestContext()
        {
            Campaigns = new Campaigns(Campaign);
        }

        public static TestContext Create() => new();

        public CreativeQualityGateService Gate() => new(Assets);

        public CampaignReviewService Review() => new(
            Campaigns,
            new SectionGeneration(),
            Gate(),
            Overrides,
            RequestContext);

        public CreativeAsset AddAsset(bool selected, bool semanticMismatch = false, int score = 85, bool withAnalysis = true)
        {
            var asset = new CreativeAsset
            {
                Id = Guid.NewGuid(),
                CampaignId = Campaign.Id,
                FileName = $"asset-{Assets.Items.Count + 1}.png",
                StoragePath = "asset.png",
                MimeType = "image/png",
                Width = 1200,
                Height = 628,
                FileSize = 100,
                IsSelected = selected,
                CreatedAt = DateTime.UtcNow.AddMinutes(Assets.Items.Count)
            };
            if (withAnalysis)
            {
                asset.Analyses.Add(Analysis(asset.Id, semanticMismatch, score));
            }

            Assets.Items.Add(asset);
            return asset;
        }

        private static CreativeAssetAnalysis Analysis(Guid assetId, bool semanticMismatch, int score)
        {
            return new CreativeAssetAnalysis
            {
                Id = Guid.NewGuid(),
                CreativeAssetId = assetId,
                Provider = "OpenRouter",
                Model = "model",
                Summary = "summary",
                VisualQualityScore = score,
                BrandFitScore = score,
                TextDensityScore = 10,
                PlacementRecommendationsJson = "{}",
                RisksJson = "[]",
                RawResponseJson = JsonSerializer.Serialize(new
                {
                    campaignFitScore = score,
                    messageConsistencyScore = score,
                    semanticMismatch
                }),
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Campanha ValidCampaign()
        {
            return new Campanha
            {
                Id = Guid.NewGuid(),
                Nome = "Campanha valida",
                TipoPublico = TipoPublicoCampanha.Familia,
                Cidade = "Rio de Janeiro",
                Estado = "RJ",
                Regiao = "Barra",
                Operadora = "Nao se aplica",
                OrcamentoDiario = 20,
                Objetivo = "Gerar contatos",
                Status = StatusCampanha.Gerada,
                TituloLandingPage = "Titulo landing valido",
                SubtituloLandingPage = "Subtitulo landing valido para campanha",
                TextoBotao = "Fale conosco",
                MensagemWhatsApp = "Ola, quero atendimento.",
                Slug = "campanha-valida",
                BeneficiosJson = JsonSerializer.Serialize(new[] { "Beneficio 1", "Beneficio 2", "Beneficio 3" }),
                PerguntasFrequentesJson = JsonSerializer.Serialize(new[]
                {
                    new FaqResponse("Como agendar avaliacao?", "Envie seus dados para nossa equipe retornar."),
                    new FaqResponse("O atendimento e personalizado?", "Sim, a avaliacao considera o objetivo de cada pessoa."),
                    new FaqResponse("Posso tirar duvidas antes?", "Sim, o contato inicial serve para orientar o agendamento.")
                }),
                PalavrasChaveJson = JsonSerializer.Serialize(new[] { "avaliacao estetica", "harmonizacao facial", "clinica estetica" }),
                PalavrasChaveNegativasJson = JsonSerializer.Serialize(new[] { "gratis", "emprego", "curso" }),
                TitulosAnunciosJson = JsonSerializer.Serialize(Enumerable.Range(1, 8).Select(x => $"Titulo {x}").ToArray()),
                DescricoesAnunciosJson = JsonSerializer.Serialize(new[] { "Descricao valida 1", "Descricao valida 2", "Descricao valida 3" }),
                DataCriacao = DateTime.UtcNow
            };
        }
    }

    private sealed class Campaigns(Campanha campaign) : ICampanhaRepository
    {
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == campaign.Id ? campaign : null);
        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<Campanha?>(null);
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]);
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>([campaign]);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class CreativeAssets : ICreativeAssetRepository
    {
        public List<CreativeAsset> Items { get; } = [];
        public int AnalysisProviderCalls { get; }
        public Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        public Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CreativeAsset>>(Items.Where(x => x.CampaignId == campaignId && !x.IsDeleted).ToArray());
        public Task AdicionarAsync(CreativeAsset asset, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarAnaliseAsync(CreativeAssetAnalysis analysis, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remover(CreativeAsset asset)
        {
            asset.IsSelected = false;
            asset.IsDeleted = true;
            asset.DeletedAt = DateTime.UtcNow;
        }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Overrides : ICreativeQualityOverrideRepository
    {
        public List<CreativeQualityOverride> Items { get; } = [];
        public Task AdicionarAsync(CreativeQualityOverride item, CancellationToken cancellationToken) { Items.Add(item); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RequestContext : IRequestContext
    {
        public string? IpHash => "ip";
        public string? UserAgent => "test";
        public string? User => "admin@leadengine.test";
    }

    private sealed class SectionGeneration : ICampaignSectionGenerationService
    {
        public Task<CampaignSectionGenerationResult> GenerateAsync(Campanha campanha, CampanhaSecao secao, string? instrucaoAdicional, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
