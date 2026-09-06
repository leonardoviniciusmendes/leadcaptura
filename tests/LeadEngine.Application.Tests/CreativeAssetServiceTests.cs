using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LeadEngine.Application.Tests;

public sealed class CreativeAssetServiceTests
{
    [Fact]
    public async Task Upload_Valido_PersisteImagem()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var result = await service.UploadAsync(campanha.Id, [Png("arte.png", 1200, 628)], CancellationToken.None);

        var asset = Assert.Single(result.Assets);
        Assert.Equal("image/png", asset.MimeType);
        Assert.Equal(1200, asset.Width);
        Assert.Equal(628, asset.Height);
        Assert.False(asset.IsSelected);
        Assert.True(File.Exists(Path.Combine(fixture.StorageRoot, asset.StoragePath.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task Upload_MimeInvalido_Falha()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [new CreativeAssetUploadItem("arte.png", "image/png", [1, 2, 3])], CancellationToken.None));

        Assert.Contains("MIME real", ex.Message);
    }

    [Fact]
    public async Task Upload_ExtensaoIncompativel_Falha()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [Png("arte.jpg", 800, 600)], CancellationToken.None));

        Assert.Contains("Extensao", ex.Message);
    }

    [Fact]
    public async Task Upload_ArquivoGrande_Falha()
    {
        using var fixture = new Fixture { MaxFileBytes = 20 };
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [Png("arte.png", 800, 600)], CancellationToken.None));

        Assert.Contains("excede", ex.Message);
    }

    [Fact]
    public async Task Upload_MultiplasImagens_PersisteTodas()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var result = await service.UploadAsync(campanha.Id, [Png("a.png", 800, 600), Png("b.png", 1080, 1080), Png("c.png", 1200, 628)], CancellationToken.None);

        Assert.Equal(3, result.Assets.Count);
        Assert.Equal(3, fixture.Assets.Assets.Count);
    }

    [Fact]
    public async Task AnaliseFake_GeraRespostaEstruturada()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Png("arte.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal("Fake", analysis.Provider);
        Assert.Equal("fake-vision-v1", analysis.Model);
        Assert.Equal("recommended", analysis.Placements["facebookFeed"]);
        Assert.Equal("LEARN_MORE", analysis.SuggestedCopy.Cta);
        Assert.False(analysis.SemanticMismatch);
        Assert.True(analysis.CampaignFitScore > 0);
        Assert.True(analysis.MessageConsistencyScore > 0);
        Assert.Empty(analysis.Risks);
    }

    [Fact]
    public async Task Analise_RespostaIaInvalida_FalhaENaoPersiste()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service(new InvalidJsonProvider());
        var asset = (await service.UploadAsync(campanha.Id, [Png("arte.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None));

        Assert.Empty(fixture.Assets.Analyses);
    }

    [Fact]
    public async Task Listar_OrdenaPorScore()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var high = fixture.Service(new StaticProvider(95, 95, 10, 95, 95, false));
        var low = fixture.Service(new StaticProvider(50, 50, 90, 50, 50, false));
        var assets = (await high.UploadAsync(campanha.Id, [Png("a.png", 1200, 628), Png("b.png", 1200, 628)], CancellationToken.None)).Assets;
        await low.AnalyzeAsync(campanha.Id, assets[0].Id, CancellationToken.None);
        await high.AnalyzeAsync(campanha.Id, assets[1].Id, CancellationToken.None);

        var result = await high.ListAsync(campanha.Id, CancellationToken.None);

        Assert.Equal(assets[1].Id, result[0].Id);
        Assert.True(result[0].RankingScore > result[1].RankingScore);
    }

    [Fact]
    public async Task Analise_IncompatibilidadeSemantica_LimitaScoreFinal()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new StaticProvider(
            92,
            65,
            80,
            18,
            12,
            true,
            "Cotacao de plano familiar. Compare planos de saude.",
            ["Imagem e texto promovem plano de saude, enquanto a campanha e sobre tratamentos esteticos."]));
        var asset = (await service.UploadAsync(campanha.Id, [Png("plano-saude.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.True(analysis.SemanticMismatch);
        Assert.True(analysis.CampaignFitScore < 30);
        Assert.True(analysis.MessageConsistencyScore < 30);
        Assert.True(analysis.RankingScore <= 35);
        Assert.Contains("plano de saude", analysis.Risks[0]);
    }

    [Fact]
    public async Task Analise_ImagemCoerente_MantemScoreFinalAlto()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new StaticProvider(
            88,
            84,
            35,
            91,
            89,
            false,
            "Harmonizacao facial com avaliacao personalizada.",
            []));
        var asset = (await service.UploadAsync(campanha.Id, [Png("estetica.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.False(analysis.SemanticMismatch);
        Assert.True(analysis.CampaignFitScore >= 85);
        Assert.True(analysis.MessageConsistencyScore >= 85);
        Assert.True(analysis.RankingScore >= 80);
    }

    [Fact]
    public async Task Selecionar_MarcaSomenteUmaImagem()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var assets = (await service.UploadAsync(campanha.Id, [Png("a.png", 800, 600), Png("b.png", 800, 600)], CancellationToken.None)).Assets;

        await service.SelectAsync(campanha.Id, assets[0].Id, CancellationToken.None);
        await service.SelectAsync(campanha.Id, assets[1].Id, CancellationToken.None);

        var listed = await service.ListAsync(campanha.Id, CancellationToken.None);
        Assert.True(listed.Single(x => x.Id == assets[1].Id).IsSelected);
        Assert.False(listed.Single(x => x.Id == assets[0].Id).IsSelected);
    }

    [Fact]
    public async Task CampanhaSemImagem_RetornaListaVaziaESelecaoFalha()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        Assert.Empty(await service.ListAsync(campanha.Id, CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SelectAsync(campanha.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Imagens_FicamIsoladasEntreCampanhas()
    {
        using var fixture = new Fixture();
        var campanhaA = fixture.Campaigns.AddCampaign();
        var campanhaB = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var assetA = (await service.UploadAsync(campanhaA.Id, [Png("a.png", 800, 600)], CancellationToken.None)).Assets.Single();
        await service.UploadAsync(campanhaB.Id, [Png("b.png", 800, 600)], CancellationToken.None);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AnalyzeAsync(campanhaB.Id, assetA.Id, CancellationToken.None));
        Assert.Single(await service.ListAsync(campanhaA.Id, CancellationToken.None));
        Assert.Single(await service.ListAsync(campanhaB.Id, CancellationToken.None));
    }

    private static CreativeAssetUploadItem Png(string name, int width, int height)
    {
        var bytes = new byte[33];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        signature.CopyTo(bytes, 0);
        bytes[12] = (byte)'I';
        bytes[13] = (byte)'H';
        bytes[14] = (byte)'D';
        bytes[15] = (byte)'R';
        WriteBigEndian(bytes, 16, width);
        WriteBigEndian(bytes, 20, height);
        return new CreativeAssetUploadItem(name, "image/png", bytes);
    }

    private static void WriteBigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xFF);
        bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 3] = (byte)(value & 0xFF);
    }

    private sealed class Fixture : IDisposable
    {
        public InMemoryCampanhaRepository Campaigns { get; } = new();
        public InMemoryCreativeAssetRepository Assets { get; } = new();
        public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), $"leadengine-assets-{Guid.NewGuid():N}");
        public long MaxFileBytes { get; init; } = 10 * 1024 * 1024;

        public CreativeAssetService Service(ICreativeAssetAnalysisProvider? provider = null)
        {
            return new CreativeAssetService(
                Campaigns,
                Assets,
                provider ?? new FakeCreativeAssetAnalysisProvider(),
                Options.Create(new CreativeAssetOptions { StorageRoot = StorageRoot, MaxFileBytes = MaxFileBytes }));
        }

        public void Dispose()
        {
            if (Directory.Exists(StorageRoot))
            {
                Directory.Delete(StorageRoot, true);
            }
        }
    }

    private sealed class InvalidJsonProvider : ICreativeAssetAnalysisProvider
    {
        public Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CreativeAssetAnalysisProviderResult("Fake", "invalid", "{}"));
        }
    }

    private sealed class StaticProvider(
        int visual,
        int brand,
        int density,
        int campaign,
        int consistency,
        bool semanticMismatch,
        string detectedText = "",
        IReadOnlyList<string>? risks = null) : ICreativeAssetAnalysisProvider
    {
        public Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
        {
            var risksJson = string.Join(",", (risks ?? []).Select(x => $"\"{x}\""));
            var json = $$"""
                {
                  "summary": "analise controlada",
                  "detectedText": "{{detectedText}}",
                  "visualQualityScore": {{visual}},
                  "campaignFitScore": {{campaign}},
                  "brandFitScore": {{brand}},
                  "textDensityScore": {{density}},
                  "messageConsistencyScore": {{consistency}},
                  "semanticMismatch": {{semanticMismatch.ToString().ToLowerInvariant()}},
                  "placementRecommendations": { "facebookFeed": "recommended" },
                  "risks": [{{risksJson}}],
                  "suggestedCopy": { "headline": "h", "primaryText": "p", "description": "d", "cta": "LEARN_MORE" }
                }
                """;
            return Task.FromResult(new CreativeAssetAnalysisProviderResult("Fake", "static", json));
        }
    }

    private sealed class InMemoryCreativeAssetRepository : ICreativeAssetRepository
    {
        public List<CreativeAsset> Assets { get; } = [];
        public List<CreativeAssetAnalysis> Analyses { get; } = [];

        public Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Assets.FirstOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CreativeAsset>>(Assets.Where(x => x.CampaignId == campaignId).OrderByDescending(x => x.CreatedAt).ToArray());
        }

        public Task AdicionarAsync(CreativeAsset asset, CancellationToken cancellationToken)
        {
            Assets.Add(asset);
            return Task.CompletedTask;
        }

        public Task AdicionarAnaliseAsync(CreativeAssetAnalysis analysis, CancellationToken cancellationToken)
        {
            Analyses.Add(analysis);
            Assets.First(x => x.Id == analysis.CreativeAssetId).Analyses.Add(analysis);
            return Task.CompletedTask;
        }

        public Task SalvarAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCampanhaRepository : ICampanhaRepository
    {
        private readonly List<Campanha> campanhas = [];

        public Campanha AddCampaign(string segmentName = "Servicos Locais", string productOrService = "Consultoria")
        {
            var campaign = new Campanha
            {
                Id = Guid.NewGuid(),
                Nome = "Campanha Teste",
                Segment = new Segment { Id = Guid.NewGuid(), Name = segmentName, Slug = segmentName.ToLowerInvariant().Replace(" ", "-"), TemplateKey = "local_service", IsActive = true },
                TipoPublico = TipoPublicoCampanha.Empresa,
                Cidade = "Rio de Janeiro",
                Estado = "RJ",
                Regiao = "Centro",
                Operadora = "Nao se aplica",
                OrcamentoDiario = 50,
                Objetivo = "Gerar leads",
                TituloLandingPage = "Titulo",
                SubtituloLandingPage = "Subtitulo",
                TextoBotao = "Contato",
                MensagemWhatsApp = "Ola",
                Slug = Guid.NewGuid().ToString("N"),
                CampaignConfigJson = JsonSerializer.Serialize(new
                {
                    businessDescription = "Empresa local",
                    productOrService,
                    targetAudience = "PMEs",
                    campaignGoal = "Gerar contatos",
                    offer = "Diagnostico",
                    brandTone = "Profissional",
                    restrictions = new[] { "nao prometer resultado" },
                    location = new { city = "Rio de Janeiro", state = "RJ", region = "Centro" }
                })
            };
            campanhas.Add(campaign);
            return campaign;
        }

        public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AdicionarRevisaoAsync(CampanhaRevisao revisao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExisteSlugAsync(string slug, Guid? ignorarId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(campanhas.FirstOrDefault(x => x.Id == id));
        public Task<Campanha?> ObterPublicadaPorSlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<Campanha?>(null);
        public Task<IReadOnlyList<CampanhaRevisao>> ListarRevisoesAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CampanhaRevisao>>([]);
        public Task<IReadOnlyList<Campanha>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Campanha>>(campanhas);
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
