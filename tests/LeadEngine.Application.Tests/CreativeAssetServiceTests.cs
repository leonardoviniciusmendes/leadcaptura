using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Buffers.Binary;
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
        using var fixture = new Fixture { MaxImageBytes = 20 };
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
    public async Task Analise_ScoresZeroACem_PermanecemIntactos()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new ScaleProvider(88, 78, 72, 100, 74, false, "Imagem coerente com a campanha."));
        var asset = (await service.UploadAsync(campanha.Id, [Png("escala-100.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal(88, analysis.VisualQualityScore);
        Assert.Equal(78, analysis.CampaignFitScore);
        Assert.Equal(72, analysis.BrandFitScore);
        Assert.Equal(100, analysis.TextDensityScore);
        Assert.Equal(74, analysis.MessageConsistencyScore);
        Assert.False(analysis.SemanticMismatch);
    }

    [Fact]
    public async Task Analise_ScoresZeroADezComEvidenciaPositiva_NormalizaParaZeroACem()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new ScaleProvider(
            9,
            10,
            10,
            9,
            10,
            false,
            "Excelente video que alinha perfeitamente com o briefing.",
            "ANTES DA AGULHA, VEM A ANALISE. ESTETICA COM CRITERIO. WhatsApp."));
        var asset = (await service.UploadAsync(campanha.Id, [Png("escala-10.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal(90, analysis.VisualQualityScore);
        Assert.Equal(100, analysis.CampaignFitScore);
        Assert.Equal(100, analysis.BrandFitScore);
        Assert.Equal(90, analysis.TextDensityScore);
        Assert.Equal(100, analysis.MessageConsistencyScore);
        Assert.False(analysis.SemanticMismatch);
        Assert.True(analysis.RankingScore >= 90);
    }

    [Fact]
    public async Task Analise_ScoresZeroADezNormalizados_NaoForcamSemanticMismatch()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new ScaleProvider(9, 10, 10, 9, 10, false, "Excelente video que alinha perfeitamente com o briefing."));
        var asset = (await service.UploadAsync(campanha.Id, [Png("coerente.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.False(analysis.SemanticMismatch);
        Assert.DoesNotContain("semanticMismatchOriginal", analysis.RawResponseJson);
    }

    [Fact]
    public async Task Analise_ScoresBaixosZeroACem_NaoConfundeComEscalaZeroADez()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new ScaleProvider(10, 5, 8, 20, 5, false, "Midia ruim e sem aderencia suficiente."));
        var asset = (await service.UploadAsync(campanha.Id, [Png("baixo.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal(10, analysis.VisualQualityScore);
        Assert.Equal(5, analysis.CampaignFitScore);
        Assert.Equal(8, analysis.BrandFitScore);
        Assert.Equal(20, analysis.TextDensityScore);
        Assert.Equal(5, analysis.MessageConsistencyScore);
        Assert.True(analysis.SemanticMismatch);
        Assert.True(analysis.RankingScore <= 35);
    }

    [Fact]
    public async Task Analise_SemanticMismatchExplicitoTruePermaneceTrueMesmoComEscalaZeroADez()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new ScaleProvider(9, 10, 10, 9, 10, true, "Conteudo promove outro segmento."));
        var asset = (await service.UploadAsync(campanha.Id, [Png("mismatch.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.True(analysis.SemanticMismatch);
        Assert.True(analysis.RankingScore <= 35);
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

    [Fact]
    public async Task Remover_AssetNormal_NaoAfetaOutrosAssets()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var assets = (await service.UploadAsync(campanha.Id, [Png("a.png", 800, 600), Png("b.png", 800, 600)], CancellationToken.None)).Assets;

        await service.RemoverAsync(campanha.Id, assets[0].Id, CancellationToken.None);

        var listed = await service.ListAsync(campanha.Id, CancellationToken.None);
        Assert.Single(listed);
        Assert.Equal(assets[1].Id, listed[0].Id);
        Assert.True(fixture.Assets.Assets.Single(x => x.Id == assets[0].Id).IsDeleted);
    }

    [Fact]
    public async Task Remover_AssetPrincipal_DeixaCampanhaSemImagemPrincipal()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var assets = (await service.UploadAsync(campanha.Id, [Png("principal.png", 800, 600), Png("outra.png", 800, 600)], CancellationToken.None)).Assets;
        await service.SelectAsync(campanha.Id, assets[0].Id, CancellationToken.None);

        await service.RemoverAsync(campanha.Id, assets[0].Id, CancellationToken.None);

        Assert.DoesNotContain(await service.ListAsync(campanha.Id, CancellationToken.None), x => x.IsSelected);
        Assert.False(fixture.Assets.Assets.Single(x => x.Id == assets[1].Id).IsSelected);
    }

    [Fact]
    public async Task Remover_AssetDeOutraCampanha_Rejeita()
    {
        using var fixture = new Fixture();
        var campanhaA = fixture.Campaigns.AddCampaign();
        var campanhaB = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanhaA.Id, [Png("a.png", 800, 600)], CancellationToken.None)).Assets.Single();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RemoverAsync(campanhaB.Id, asset.Id, CancellationToken.None));

        Assert.False(fixture.Assets.Assets.Single().IsDeleted);
    }

    [Fact]
    public async Task Remover_ArquivoFisicoExistente_RemoveDoStorage()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Png("arquivo.png", 800, 600)], CancellationToken.None)).Assets.Single();
        var path = Path.Combine(fixture.StorageRoot, asset.StoragePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path));

        await service.RemoverAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Remover_AnalisesPermanecemVinculadasAoAssetHistorico()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Png("analise.png", 800, 600)], CancellationToken.None)).Assets.Single();
        await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        await service.RemoverAsync(campanha.Id, asset.Id, CancellationToken.None);

        var analysis = Assert.Single(fixture.Assets.Analyses);
        Assert.Equal(asset.Id, analysis.CreativeAssetId);
        Assert.Contains(fixture.Assets.Assets, x => x.Id == asset.Id && x.IsDeleted);
    }

    [Fact]
    public async Task Remover_AssetPrincipalUnico_QualityGateRetornaNoCreative()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Png("principal.png", 800, 600)], CancellationToken.None)).Assets.Single();
        await service.SelectAsync(campanha.Id, asset.Id, CancellationToken.None);

        await service.RemoverAsync(campanha.Id, asset.Id, CancellationToken.None);
        var gate = await new CreativeQualityGateService(fixture.Assets).EvaluateAsync(campanha.Id, CancellationToken.None);

        Assert.Equal("NO_CREATIVE", gate.Status);
    }

    [Fact]
    public async Task Remover_DesvinculaMetaAdsImagemLocalDoMesmoConteudo()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Png("meta.png", 800, 600)], CancellationToken.None)).Assets.Single();
        var path = Path.Combine(fixture.StorageRoot, asset.StoragePath.Replace('/', Path.DirectorySeparatorChar));
        var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)));
        fixture.MetaImages.Images.Add(new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            OrigemImagem = "CreativeAsset",
            ContentHash = contentHash,
            MetaImageHash = "hash",
            NomeArquivo = "meta.png"
        });

        await service.RemoverAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Empty(fixture.MetaImages.Images);
    }

    [Fact]
    public async Task Upload_Mp4Valido_PersisteVideoComMetadadosEThumbnail()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var result = await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 1080, 1920, 12)], CancellationToken.None);

        var asset = Assert.Single(result.Assets);
        Assert.Equal("Video", asset.MediaType);
        Assert.Equal("video/mp4", asset.MimeType);
        Assert.Equal(1080, asset.Width);
        Assert.Equal(1920, asset.Height);
        Assert.Equal(12, Math.Round(asset.DurationSeconds!.Value));
        Assert.NotNull(asset.ThumbnailUrl);
        Assert.True(File.Exists(Path.Combine(fixture.StorageRoot, fixture.Assets.Assets.Single().ThumbnailPath!.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task Upload_Video_UsaMetadadosDoFfprobeQuandoDisponivel()
    {
        using var fixture = new Fixture();
        fixture.Video.Metadata = new VideoMetadata(17.345, 478, 850, "h264", 29.97, 0);
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var result = await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None);

        var asset = Assert.Single(result.Assets);
        Assert.Equal(478, asset.Width);
        Assert.Equal(850, asset.Height);
        Assert.Equal(17.345, asset.DurationSeconds!.Value, 3);
    }

    [Fact]
    public async Task Upload_WebmValido_PersisteVideo()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var result = await service.UploadAsync(campanha.Id, [Webm("video.webm", 1280, 720, 8)], CancellationToken.None);

        var asset = Assert.Single(result.Assets);
        Assert.Equal("Video", asset.MediaType);
        Assert.Equal("video/webm", asset.MimeType);
        Assert.Equal(1280, asset.Width);
        Assert.Equal(720, asset.Height);
    }

    [Fact]
    public async Task Upload_Mp4Falso_Rejeita()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [new CreativeAssetUploadItem("fake.mp4", "video/mp4", [1, 2, 3, 4])], CancellationToken.None));

        Assert.Contains("MIME real", ex.Message);
    }

    [Fact]
    public async Task Upload_VideoTamanhoMaximo_Rejeita()
    {
        using var fixture = new Fixture { MaxVideoBytes = 40 };
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None));

        Assert.Contains("Video excede", ex.Message);
    }

    [Fact]
    public async Task Upload_VideoDuracaoMaxima_Rejeita()
    {
        using var fixture = new Fixture { MaxVideoDurationSeconds = 5 };
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None));

        Assert.Contains("duracao maxima", ex.Message);
    }

    [Fact]
    public async Task AnaliseVideo_EnviaFramesENaoEnviaVideoInteiro()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var provider = new CapturingProvider(90, false);
        var service = fixture.Service(provider);
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal("Video", provider.LastRequest!.MediaType);
        Assert.Null(provider.LastRequest.Content);
        Assert.NotEmpty(provider.LastRequest.Frames);
        Assert.True(provider.LastRequest.Frames.Count <= 5);
        Assert.False(analysis.SemanticMismatch);
        Assert.True(analysis.RankingScore >= 70);
    }

    [Fact]
    public async Task AnaliseVideo_SemFramesReais_FalhaSemChamarProvider()
    {
        using var fixture = new Fixture();
        fixture.Video.ReturnFrames = false;
        var campanha = fixture.Campaigns.AddCampaign();
        var provider = new CapturingProvider(90, false);
        var service = fixture.Service(provider);
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None));

        Assert.Contains("Processamento de video nao esta disponivel", ex.Message);
        Assert.Null(provider.LastRequest);
    }

    [Fact]
    public async Task AnaliseVideo_EnviaTimestampsSequenciaisDosFrames()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var provider = new CapturingProvider(88, false);
        var service = fixture.Service(provider);
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();

        await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.Equal([1d, 3d, 5d], provider.LastRequest!.Frames.Select(x => Math.Round(x.OffsetSeconds, 1)).ToArray());
    }

    [Fact]
    public async Task Analise_ModeloRetornaFalseMasScoresMuitoBaixos_NormalizaSemanticMismatch()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign("Estetica", "Harmonizacao facial");
        var service = fixture.Service(new StaticProvider(
            20,
            20,
            15,
            5,
            5,
            false,
            "conteudo de outro segmento",
            ["Resumo indica baixa aderencia ao briefing."]));
        var asset = (await service.UploadAsync(campanha.Id, [Png("incompativel.png", 1200, 628)], CancellationToken.None)).Assets.Single();

        var analysis = await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.True(analysis.SemanticMismatch);
        Assert.True(analysis.RankingScore <= 35);
        Assert.DoesNotContain("semanticMismatchOriginal", analysis.RawResponseJson);
        Assert.DoesNotContain("semanticMismatchReason", analysis.RawResponseJson);
    }

    [Fact]
    public async Task AnaliseVideo_Incompativel_GeraBlockedNoQualityGate()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service(new CapturingProvider(18, true));
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("plano-saude.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();
        await service.SelectAsync(campanha.Id, asset.Id, CancellationToken.None);
        await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        var gate = await new CreativeQualityGateService(fixture.Assets).EvaluateAsync(campanha.Id, CancellationToken.None);

        Assert.Equal("BLOCKED", gate.Status);
        Assert.True(gate.SemanticMismatch);
    }

    [Fact]
    public async Task AnaliseVideo_Coerente_GeraApprovedNoQualityGate()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service(new CapturingProvider(88, false));
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("estetica.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();
        await service.SelectAsync(campanha.Id, asset.Id, CancellationToken.None);
        await service.AnalyzeAsync(campanha.Id, asset.Id, CancellationToken.None);

        var gate = await new CreativeQualityGateService(fixture.Assets).EvaluateAsync(campanha.Id, CancellationToken.None);

        Assert.Equal("APPROVED", gate.Status);
    }

    [Fact]
    public async Task Selecionar_ImageVideo_AlternaPrincipal()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var image = (await service.UploadAsync(campanha.Id, [Png("imagem.png", 800, 600)], CancellationToken.None)).Assets.Single();
        var video = (await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();

        await service.SelectAsync(campanha.Id, video.Id, CancellationToken.None);
        Assert.True((await service.ListAsync(campanha.Id, CancellationToken.None)).Single(x => x.Id == video.Id).IsSelected);

        await service.SelectAsync(campanha.Id, image.Id, CancellationToken.None);
        var listed = await service.ListAsync(campanha.Id, CancellationToken.None);
        Assert.True(listed.Single(x => x.Id == image.Id).IsSelected);
        Assert.False(listed.Single(x => x.Id == video.Id).IsSelected);
    }

    [Fact]
    public async Task Remover_Video_RemoveArquivoEThumbnailENaoLista()
    {
        using var fixture = new Fixture();
        var campanha = fixture.Campaigns.AddCampaign();
        var service = fixture.Service();
        var asset = (await service.UploadAsync(campanha.Id, [Mp4("video.mp4", 640, 360, 10)], CancellationToken.None)).Assets.Single();
        var entity = fixture.Assets.Assets.Single();
        var videoPath = Path.Combine(fixture.StorageRoot, entity.StoragePath.Replace('/', Path.DirectorySeparatorChar));
        var thumbnailPath = Path.Combine(fixture.StorageRoot, entity.ThumbnailPath!.Replace('/', Path.DirectorySeparatorChar));

        await service.RemoverAsync(campanha.Id, asset.Id, CancellationToken.None);

        Assert.False(File.Exists(videoPath));
        Assert.False(File.Exists(thumbnailPath));
        Assert.Empty(await service.ListAsync(campanha.Id, CancellationToken.None));
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

    private static CreativeAssetUploadItem Mp4(string name, int width, int height, int durationSeconds)
    {
        var ftyp = Box("ftyp", [.. "isom"u8.ToArray(), 0, 0, 0, 1, .. "isom"u8.ToArray()]);
        var mvhd = new byte[100];
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(12, 4), 1000);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(16, 4), (uint)(durationSeconds * 1000));
        var tkhd = new byte[100];
        BinaryPrimitives.WriteUInt32BigEndian(tkhd.AsSpan(76, 4), (uint)(width << 16));
        BinaryPrimitives.WriteUInt32BigEndian(tkhd.AsSpan(80, 4), (uint)(height << 16));
        var trak = Box("trak", Box("tkhd", tkhd));
        var moov = Box("moov", [.. Box("mvhd", mvhd), .. trak]);
        return new CreativeAssetUploadItem(name, "video/mp4", [.. ftyp, .. moov]);
    }

    private static CreativeAssetUploadItem Webm(string name, int width, int height, int durationSeconds)
    {
        var duration = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(duration, BitConverter.DoubleToInt64Bits(durationSeconds));
        var bytes = new List<byte> { 0x1A, 0x45, 0xDF, 0xA3, 0x44, 0x89, 0x08 };
        bytes.AddRange(duration);
        bytes.AddRange([0xB0, 0x02, (byte)(width >> 8), (byte)width]);
        bytes.AddRange([0xBA, 0x02, (byte)(height >> 8), (byte)height]);
        return new CreativeAssetUploadItem(name, "video/webm", bytes.ToArray());
    }

    private static byte[] Box(string type, byte[] payload)
    {
        var bytes = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), (uint)bytes.Length);
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(bytes, 4);
        payload.CopyTo(bytes, 8);
        return bytes;
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
        public InMemoryMetaAdsImagemRepository MetaImages { get; } = new();
        public VideoProcessing Video { get; } = new();
        public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), $"leadengine-assets-{Guid.NewGuid():N}");
        public long MaxFileBytes { get; init; } = 10 * 1024 * 1024;
        public long MaxImageBytes { get; init; } = 10 * 1024 * 1024;
        public long MaxVideoBytes { get; init; } = 100 * 1024 * 1024;
        public int MaxVideoDurationSeconds { get; init; } = 120;

        public CreativeAssetService Service(ICreativeAssetAnalysisProvider? provider = null)
        {
            return new CreativeAssetService(
                Campaigns,
                Assets,
                provider ?? new FakeCreativeAssetAnalysisProvider(),
                MetaImages,
                Video,
                NullLogger<CreativeAssetService>.Instance,
                Options.Create(new CreativeAssetOptions
                {
                    StorageRoot = StorageRoot,
                    MaxFileBytes = MaxFileBytes,
                    MaxImageBytes = MaxImageBytes,
                    MaxVideoBytes = MaxVideoBytes,
                    MaxVideoDurationSeconds = MaxVideoDurationSeconds
                }));
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

    private sealed class VideoProcessing : IVideoProcessingService
    {
        public VideoMetadata? Metadata { get; set; }
        public bool ReturnFrames { get; set; } = true;

        public Task<VideoMetadata?> ProbeAsync(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(Metadata);
        }

        public async Task<string?> ExtractPosterAsync(string videoPath, string outputPath, double durationSeconds, CancellationToken cancellationToken)
        {
            await File.WriteAllBytesAsync(outputPath, Bytes(Png("poster.png", 320, 180)), cancellationToken);
            return outputPath;
        }

        public Task<IReadOnlyList<CreativeAssetAnalysisFrame>> ExtractAnalysisFramesAsync(string videoPath, double durationSeconds, CancellationToken cancellationToken)
        {
            if (!ReturnFrames)
            {
                return Task.FromResult<IReadOnlyList<CreativeAssetAnalysisFrame>>([]);
            }

            IReadOnlyList<CreativeAssetAnalysisFrame> frames =
            [
                new("frame-1", durationSeconds * 0.10, "image/png", Bytes(Png("f1.png", 320, 180))),
                new("frame-2", durationSeconds * 0.30, "image/png", Bytes(Png("f2.png", 320, 180))),
                new("frame-3", durationSeconds * 0.50, "image/png", Bytes(Png("f3.png", 320, 180)))
            ];
            return Task.FromResult(frames);
        }
    }

    private static byte[] Bytes(CreativeAssetUploadItem item)
    {
        using var memory = new MemoryStream();
        item.Content.Position = 0;
        item.Content.CopyTo(memory);
        return memory.ToArray();
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

    private sealed class ScaleProvider(
        int visual,
        int campaign,
        int brand,
        int density,
        int consistency,
        bool semanticMismatch,
        string summary,
        string detectedText = "") : ICreativeAssetAnalysisProvider
    {
        public Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
        {
            var json = $$"""
                {
                  "summary": "{{summary}}",
                  "detectedText": "{{detectedText}}",
                  "visualQualityScore": {{visual}},
                  "campaignFitScore": {{campaign}},
                  "brandFitScore": {{brand}},
                  "textDensityScore": {{density}},
                  "messageConsistencyScore": {{consistency}},
                  "semanticMismatch": {{semanticMismatch.ToString().ToLowerInvariant()}},
                  "placementRecommendations": {
                    "facebookFeed": "recommended",
                    "instagramFeed": "recommended",
                    "stories": "recommended",
                    "reels": "recommended"
                  },
                  "risks": [],
                  "suggestedCopy": { "headline": "h", "primaryText": "p", "description": "d", "cta": "LEARN_MORE" }
                }
                """;
            return Task.FromResult(new CreativeAssetAnalysisProviderResult("Fake", "scale-test", json));
        }
    }

    private sealed class CapturingProvider(int score, bool semanticMismatch) : ICreativeAssetAnalysisProvider
    {
        public CreativeAssetAnalysisProviderRequest? LastRequest { get; private set; }

        public Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var json = $$"""
                {
                  "summary": "video analisado",
                  "detectedText": "",
                  "visualQualityScore": {{score}},
                  "campaignFitScore": {{score}},
                  "brandFitScore": {{score}},
                  "textDensityScore": 20,
                  "messageConsistencyScore": {{score}},
                  "semanticMismatch": {{semanticMismatch.ToString().ToLowerInvariant()}},
                  "placementRecommendations": { "facebookFeed": "recommended", "instagramFeed": "recommended", "stories": "recommended", "reels": "recommended" },
                  "risks": [],
                  "suggestedCopy": { "headline": "h", "primaryText": "p", "description": "d", "cta": "LEARN_MORE" }
                }
                """;
            return Task.FromResult(new CreativeAssetAnalysisProviderResult("Fake", "video-test", json));
        }
    }

    private sealed class InMemoryCreativeAssetRepository : ICreativeAssetRepository
    {
        public List<CreativeAsset> Assets { get; } = [];
        public List<CreativeAssetAnalysis> Analyses { get; } = [];

        public Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Assets.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        }

        public Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CreativeAsset>>(Assets.Where(x => x.CampaignId == campaignId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToArray());
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

        public void Remover(CreativeAsset asset)
        {
            asset.IsSelected = false;
            asset.IsDeleted = true;
            asset.DeletedAt = DateTime.UtcNow;
        }

        public Task SalvarAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMetaAdsImagemRepository : IMetaAdsImagemRepository
    {
        public List<MetaAdsImagem> Images { get; } = [];
        public Task<MetaAdsImagem?> ObterPorCampanhaAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken) => Task.FromResult(Images.FirstOrDefault(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId));
        public Task<MetaAdsImagem?> ObterPorConteudoAsync(Guid campanhaId, string adAccountId, string contentHash, CancellationToken cancellationToken) => Task.FromResult(Images.FirstOrDefault(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId && x.ContentHash == contentHash));
        public Task AdicionarAsync(MetaAdsImagem imagem, CancellationToken cancellationToken) { Images.Add(imagem); return Task.CompletedTask; }
        public Task RemoverPorConteudoAsync(Guid campanhaId, string contentHash, string origemImagem, CancellationToken cancellationToken)
        {
            Images.RemoveAll(x => x.CampanhaId == campanhaId && x.ContentHash == contentHash && x.OrigemImagem == origemImagem);
            return Task.CompletedTask;
        }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
