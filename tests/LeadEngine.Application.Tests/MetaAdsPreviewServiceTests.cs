using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

public sealed class MetaAdsPreviewServiceTests
{
    [Fact]
    public async Task Preview_ComCreativeAssetSelecionado_UsaAssetEEnviaParaMeta()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("teste.png", selected: true);

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("CreativeAsset", preview.Creative.MediaSource);
        Assert.Equal(asset.Id, preview.Creative.CreativeAssetId);
        Assert.Equal("teste.png", preview.Creative.FileName);
        Assert.Equal("meta_hash_teste.png", preview.Creative.MetaImageHash);
        Assert.True(preview.Creative.MediaUploaded);
        Assert.Equal(1, ctx.Graph.Uploads);
        Assert.Equal("teste.png", ctx.Graph.LastUploadFileName);
    }

    [Fact]
    public async Task Preview_SemCreativeAsset_UsaFallbackMetaAdsImagem()
    {
        using var ctx = PreviewContext.Create();
        ctx.MetaImages.Images.Add(new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            AdAccountId = "act_1",
            NomeArquivo = "fallback.png",
            ContentType = "image/png",
            ContentHash = "fallback",
            MetaImageHash = "fallback_hash",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("MetaAdsImagem", preview.Creative.MediaSource);
        Assert.Null(preview.Creative.CreativeAssetId);
        Assert.Equal("fallback.png", preview.Creative.FileName);
        Assert.Equal("fallback_hash", preview.Creative.MetaImageHash);
        Assert.Equal(0, ctx.Graph.Uploads);
    }

    [Fact]
    public async Task Preview_CreativeAssetSemArquivo_RetornaErroControladoNoPreflight()
    {
        using var ctx = PreviewContext.Create();
        ctx.AddCreativeAsset("sumiu.png", selected: true, writeFile: false);

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.False(preview.Preflight.ReadyToPublish);
        Assert.Contains(preview.Preflight.Items, x => x.Code == "MediaValid" && x.Status == "ERROR" && x.Message.Contains("nao foi encontrado"));
        Assert.Equal("CreativeAsset", preview.Creative.MediaSource);
        Assert.Null(preview.Creative.MetaImageHash);
        Assert.Equal(0, ctx.Graph.Uploads);
    }

    [Fact]
    public async Task Preview_SemanticMismatch_GeraWarningSemBloquearPorAnalise()
    {
        using var ctx = PreviewContext.Create();
        ctx.AddCreativeAsset("mismatch.png", selected: true, semanticMismatch: true, campaignFit: 1, consistency: 1);

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Contains(preview.Preflight.Items, x => x.Code == "CreativeSemanticMismatch" && x.Status == "WARNING");
        Assert.True(preview.Creative.SemanticMismatch);
        Assert.True(preview.Preflight.Items.Where(x => x.Code.StartsWith("Creative", StringComparison.Ordinal)).All(x => x.Status == "WARNING" || x.Status == "OK"));
    }

    [Fact]
    public async Task Preview_ScoreBaixo_GeraWarning()
    {
        using var ctx = PreviewContext.Create();
        ctx.AddCreativeAsset("baixo.png", selected: true, campaignFit: 10, consistency: 10, visual: 10, brand: 10);

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal(10, preview.Creative.AnalysisScore);
        Assert.Contains(preview.Preflight.Items, x => x.Code == "CreativeLowAnalysisScore" && x.Status == "WARNING");
    }

    [Fact]
    public async Task Preview_TrocaCreativeAsset_UsaNovoAsset()
    {
        using var ctx = PreviewContext.Create();
        var oldAsset = ctx.AddCreativeAsset("antigo.png", selected: true);
        var newAsset = ctx.AddCreativeAsset("novo.png", selected: false);

        var first = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);
        oldAsset.IsSelected = false;
        newAsset.IsSelected = true;
        var second = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal(oldAsset.Id, first.Creative.CreativeAssetId);
        Assert.Equal("meta_hash_antigo.png", first.Creative.MetaImageHash);
        Assert.Equal(newAsset.Id, second.Creative.CreativeAssetId);
        Assert.Equal("meta_hash_novo.png", second.Creative.MetaImageHash);
    }

    [Fact]
    public async Task Preview_ReutilizaImageHashSomenteParaMesmoConteudoNaMesmaAdAccount()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("atual.png", selected: true);
        ctx.MetaImages.Images.Add(new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            AdAccountId = "act_1",
            OrigemImagem = "CreativeAsset",
            NomeArquivo = "atual.png",
            ContentType = "image/png",
            TamanhoBytes = asset.FileSize,
            ContentHash = PreviewContext.ContentHash(asset),
            MetaImageHash = "hash_reutilizado",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal(asset.Id, preview.Creative.CreativeAssetId);
        Assert.Equal("hash_reutilizado", preview.Creative.MetaImageHash);
        Assert.Equal(0, ctx.Graph.Uploads);
    }

    [Fact]
    public async Task Preview_PublicacaoPlanejadaContinuaPaused()
    {
        using var ctx = PreviewContext.Create();
        ctx.AddCreativeAsset("paused.png", selected: true);

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("PAUSED", preview.Campaign.Status);
        Assert.Equal("PAUSED", preview.Ad.Status);
    }

    [Fact]
    public async Task Preview_CreativeAssetRemovido_NaoReutilizaMetaAdsImagemDoAsset()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("removido.png", selected: true);
        asset.IsSelected = false;
        asset.IsDeleted = true;
        ctx.MetaImages.Images.Add(new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            AdAccountId = "act_1",
            OrigemImagem = "CreativeAsset",
            NomeArquivo = "removido.png",
            ContentType = "image/png",
            ContentHash = PreviewContext.ContentHash(asset),
            MetaImageHash = "hash_removido",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Null(preview.Creative.CreativeAssetId);
        Assert.Null(preview.Creative.MetaImageHash);
        Assert.False(preview.Creative.MediaUploaded);
        Assert.Equal(0, ctx.Graph.Uploads);
    }

    [Fact]
    public async Task Preview_SemCreativeAsset_ContinuaUsandoFallbackMetaAdsImagemLegado()
    {
        using var ctx = PreviewContext.Create();
        ctx.MetaImages.Images.Add(new MetaAdsImagem
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            AdAccountId = "act_1",
            OrigemImagem = "UploadManual",
            NomeArquivo = "fallback-legado.png",
            ContentType = "image/png",
            ContentHash = "fallback-legado",
            MetaImageHash = "fallback_legado_hash",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("MetaAdsImagem", preview.Creative.MediaSource);
        Assert.Equal("fallback_legado_hash", preview.Creative.MetaImageHash);
        Assert.Equal(0, ctx.Graph.Uploads);
    }

    [Fact]
    public async Task Preview_CreativeAssetVideo_NaoTentaImageHash()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("video.mp4", selected: true);
        asset.MediaType = CreativeAssetMediaType.Video;
        asset.MimeType = "video/mp4";
        asset.DurationSeconds = 15;

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("CreativeAsset", preview.Creative.MediaSource);
        Assert.Equal("Video", preview.Creative.MediaType);
        Assert.Equal(asset.Id, preview.Creative.CreativeAssetId);
        Assert.Null(preview.Creative.MetaImageHash);
        Assert.Equal("video_id_video.mp4", preview.Creative.MetaVideoId);
        Assert.True(preview.Creative.MediaUploaded);
        Assert.False(preview.Creative.VideoIdReused);
        Assert.False(preview.Creative.VideoUploadRequired);
        Assert.Equal(0, ctx.Graph.Uploads);
        Assert.Equal(1, ctx.Graph.VideoUploads);
        Assert.Contains(preview.Preflight.Items, x => x.Code == "MetaVideoIdAvailable" && x.Status == "OK");
        Assert.DoesNotContain(preview.Preflight.Items, x => x.Message.Contains("ainda nao habilitada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_CreativeAssetVideo_ReutilizaMetaVideoIdPorConteudoEAdAccount()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("video-reuso.mp4", selected: true);
        asset.MediaType = CreativeAssetMediaType.Video;
        asset.MimeType = "video/mp4";
        ctx.MetaVideos.Videos.Add(new MetaAdsVideo
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            CreativeAssetId = asset.Id,
            AdAccountId = "act_1",
            NomeArquivo = asset.FileName,
            ContentType = asset.MimeType,
            ContentHash = PreviewContext.ContentHash(asset),
            MetaVideoId = "video_reutilizado",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("video_reutilizado", preview.Creative.MetaVideoId);
        Assert.True(preview.Creative.VideoIdReused);
        Assert.Equal(0, ctx.Graph.VideoUploads);
    }

    [Fact]
    public async Task Preview_CreativeAssetVideo_NaoReutilizaMetaVideoIdDeOutraAdAccount()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("video-outra-conta.mp4", selected: true);
        asset.MediaType = CreativeAssetMediaType.Video;
        asset.MimeType = "video/mp4";
        ctx.Selecao.AdAccountId = "act_2";
        ctx.MetaVideos.Videos.Add(new MetaAdsVideo
        {
            Id = Guid.NewGuid(),
            CampanhaId = ctx.Campaign.Id,
            MetaAdsContaId = ctx.Conta.Id,
            CreativeAssetId = asset.Id,
            AdAccountId = "act_1",
            NomeArquivo = asset.FileName,
            ContentType = asset.MimeType,
            ContentHash = PreviewContext.ContentHash(asset),
            MetaVideoId = "video_conta_1",
            DataUpload = DateTime.UtcNow
        });

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal("video_id_video-outra-conta.mp4", preview.Creative.MetaVideoId);
        Assert.False(preview.Creative.VideoIdReused);
        Assert.Equal(1, ctx.Graph.VideoUploads);
    }

    [Fact]
    public async Task Preview_CreativeAssetVideo_BlockedNaoFazUpload()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("video-blocked.mp4", selected: true, semanticMismatch: true, campaignFit: 5, consistency: 5, visual: 5, brand: 5);
        asset.MediaType = CreativeAssetMediaType.Video;
        asset.MimeType = "video/mp4";

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.False(preview.Preflight.ReadyToPublish);
        Assert.Equal("BLOCKED", preview.Creative.QualityGateStatus);
        Assert.Null(preview.Creative.MetaVideoId);
        Assert.True(preview.Creative.VideoUploadRequired);
        Assert.Equal(0, ctx.Graph.VideoUploads);
        Assert.Contains(preview.Preflight.Items, x => x.Code == "CreativeQualityGate" && x.Status == "ERROR");
    }

    [Fact]
    public async Task Preview_CreativeAssetVideo_TimeoutNoUploadRetornaErroControlado()
    {
        using var ctx = PreviewContext.Create();
        var asset = ctx.AddCreativeAsset("video-timeout.mp4", selected: true);
        asset.MediaType = CreativeAssetMediaType.Video;
        asset.MimeType = "video/mp4";
        ctx.Graph.ThrowVideoUploadTimeout = true;

        var preview = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.False(preview.Preflight.ReadyToPublish);
        Assert.Null(preview.Creative.MetaVideoId);
        Assert.True(preview.Creative.VideoUploadRequired);
        Assert.Empty(ctx.MetaVideos.Videos);
        Assert.Equal(0, ctx.Graph.Uploads);
        Assert.Contains(preview.Preflight.Items, x => x.Code == "MediaUploaded" && x.Status == "ERROR" && x.Message.Contains("Timeout"));
    }

    [Fact]
    public async Task Preview_TrocaVideoAtualizaMetaVideoId()
    {
        using var ctx = PreviewContext.Create();
        var oldAsset = ctx.AddCreativeAsset("video-antigo.mp4", selected: true);
        oldAsset.MediaType = CreativeAssetMediaType.Video;
        oldAsset.MimeType = "video/mp4";
        var newAsset = ctx.AddCreativeAsset("video-novo.mp4", selected: false);
        newAsset.MediaType = CreativeAssetMediaType.Video;
        newAsset.MimeType = "video/mp4";

        var first = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);
        oldAsset.IsSelected = false;
        newAsset.IsSelected = true;
        var second = await ctx.Service().GerarAsync(ctx.Request(), CancellationToken.None);

        Assert.Equal(oldAsset.Id, first.Creative.CreativeAssetId);
        Assert.Equal("video_id_video-antigo.mp4", first.Creative.MetaVideoId);
        Assert.Equal(newAsset.Id, second.Creative.CreativeAssetId);
        Assert.Equal("video_id_video-novo.mp4", second.Creative.MetaVideoId);
    }

    private sealed class PreviewContext : IDisposable
    {
        public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), $"leadengine-meta-preview-{Guid.NewGuid():N}");
        public Campanha Campaign { get; } = new()
        {
            Id = Guid.NewGuid(),
            Nome = "Campanha",
            Cidade = "Rio de Janeiro",
            Estado = "RJ",
            OrcamentoDiario = 20,
            Objetivo = "Gerar contatos",
            TituloLandingPage = "Headline",
            SubtituloLandingPage = "Texto principal",
            Slug = "campanha",
            Publicada = true,
            UrlPublica = "https://example.com/lp/campanha"
        };
        public MetaAdsConta Conta { get; } = new() { Id = Guid.NewGuid(), Ativa = true, AccessTokenProtegido = "token" };
        public MetaAdsAtivoSelecionado Selecao { get; }
        public Campaigns Campaigns { get; }
        public Contas Contas { get; }
        public Selecoes Selecoes { get; }
        public CreativeAssets Assets { get; } = new();
        public MetaImages MetaImages { get; } = new();
        public MetaVideos MetaVideos { get; } = new();
        public Preparacoes Preparacoes { get; } = new();
        public Graph Graph { get; } = new();
        public Resolver Resolver { get; } = new();
        public Protector Protector { get; } = new();

        private PreviewContext()
        {
            Directory.CreateDirectory(StorageRoot);
            Selecao = new MetaAdsAtivoSelecionado
            {
                Id = Guid.NewGuid(),
                MetaAdsContaId = Conta.Id,
                BusinessId = "business_1",
                AdAccountId = "act_1",
                PageId = "page_1"
            };
            Campaigns = new Campaigns(Campaign);
            Contas = new Contas(Conta);
            Selecoes = new Selecoes(Selecao);
        }

        public static PreviewContext Create() => new();

        public MetaAdsPreviewRequest Request() => new(Campaign.Id, LocationKey: "loc_1");

        public MetaAdsPreviewService Service() => new(
            Campaigns,
            Contas,
            Selecoes,
            MetaImages,
            MetaVideos,
            Assets,
            Preparacoes,
            Graph,
            Resolver,
            Protector,
            new CampaignPublicUrlBuilder(Resolver),
            new CreativeQualityGateService(Assets),
            Options.Create(new CreativeAssetOptions { StorageRoot = StorageRoot }));

        public CreativeAsset AddCreativeAsset(
            string fileName,
            bool selected,
            bool writeFile = true,
            bool semanticMismatch = false,
            int campaignFit = 90,
            int consistency = 90,
            int visual = 90,
            int brand = 90)
        {
            var id = Guid.NewGuid();
            var width = 1200 + Assets.Items.Count;
            var bytes = PngBytes(width, 628);
            var storagePath = $"creative-assets/{id:N}.png";
            if (writeFile)
            {
                var fullPath = Path.Combine(StorageRoot, storagePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllBytes(fullPath, bytes);
            }

            var asset = new CreativeAsset
            {
                Id = id,
                CampaignId = Campaign.Id,
                FileName = fileName,
                StoragePath = storagePath,
                MimeType = "image/png",
                Width = width,
                Height = 628,
                FileSize = bytes.LongLength,
                IsSelected = selected,
                CreatedAt = DateTime.UtcNow
            };
            asset.Analyses.Add(new CreativeAssetAnalysis
            {
                Id = Guid.NewGuid(),
                CreativeAssetId = id,
                Provider = "OpenRouter",
                Model = "model",
                Summary = "summary",
                VisualQualityScore = visual,
                BrandFitScore = brand,
                TextDensityScore = 10,
                PlacementRecommendationsJson = "{}",
                RisksJson = "[]",
                RawResponseJson = $$"""{"campaignFitScore":{{campaignFit}},"messageConsistencyScore":{{consistency}},"semanticMismatch":{{semanticMismatch.ToString().ToLowerInvariant()}}}""",
                CreatedAt = DateTime.UtcNow
            });
            Assets.Items.Add(asset);
            return asset;
        }

        public static string ContentHash(CreativeAsset asset)
        {
            var bytes = PngBytes(asset.Width, asset.Height);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        }

        private static byte[] PngBytes(int width, int height)
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
            return bytes;
        }

        private static void WriteBigEndian(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)((value >> 24) & 0xFF);
            bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
            bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
            bytes[offset + 3] = (byte)(value & 0xFF);
        }

        public void Dispose()
        {
            if (Directory.Exists(StorageRoot))
            {
                Directory.Delete(StorageRoot, true);
            }
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

    private sealed class MetaImages : IMetaAdsImagemRepository
    {
        public List<MetaAdsImagem> Images { get; } = [];
        public Task<MetaAdsImagem?> ObterPorCampanhaAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken) => Task.FromResult(Images.Where(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId).OrderByDescending(x => x.DataUpload).FirstOrDefault());
        public Task<MetaAdsImagem?> ObterPorConteudoAsync(Guid campanhaId, string adAccountId, string contentHash, CancellationToken cancellationToken) => Task.FromResult(Images.FirstOrDefault(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId && x.ContentHash == contentHash));
        public Task AdicionarAsync(MetaAdsImagem imagem, CancellationToken cancellationToken) { Images.Add(imagem); return Task.CompletedTask; }
        public Task RemoverPorConteudoAsync(Guid campanhaId, string contentHash, string origemImagem, CancellationToken cancellationToken)
        {
            Images.RemoveAll(x => x.CampanhaId == campanhaId && x.ContentHash == contentHash && x.OrigemImagem == origemImagem);
            return Task.CompletedTask;
        }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class MetaVideos : IMetaAdsVideoRepository
    {
        public List<MetaAdsVideo> Videos { get; } = [];
        public Task<MetaAdsVideo?> ObterPorConteudoAsync(string adAccountId, string contentHash, CancellationToken cancellationToken) => Task.FromResult(Videos.FirstOrDefault(x => x.AdAccountId == adAccountId && x.ContentHash == contentHash));
        public Task AdicionarAsync(MetaAdsVideo video, CancellationToken cancellationToken) { Videos.Add(video); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

    private sealed class Preparacoes : IMetaAdsPreparacaoPublicacaoRepository
    {
        public Task<MetaAdsPreparacaoPublicacao?> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken) => Task.FromResult<MetaAdsPreparacaoPublicacao?>(null);
        public Task AdicionarAsync(MetaAdsPreparacaoPublicacao preparacao, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Graph : IMetaAdsGraphClient
    {
        public int Uploads { get; private set; }
        public int VideoUploads { get; private set; }
        public string? LastUploadFileName { get; private set; }
        public bool ThrowVideoUploadTimeout { get; set; }
        public Task<IReadOnlyList<MetaAdsBusinessResponse>> ListBusinessesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MetaAdsBusinessResponse>>([new("business_1", "Business")]);
        public Task<IReadOnlyList<MetaAdsAdAccountResponse>> ListAdAccountsAsync(MetaAdsConfiguration config, string accessToken, string businessId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MetaAdsAdAccountResponse>>([new("act_1", "1", "Conta", "ACTIVE", "BRL")]);
        public Task<MetaAdsPageResponse?> GetPageAsync(MetaAdsConfiguration config, string accessToken, string pageId, CancellationToken cancellationToken) => Task.FromResult<MetaAdsPageResponse?>(new("page_1", "Page", null));
        public Task<MetaAdsPermissionStatusResponse> GetPermissionsAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => Task.FromResult(new MetaAdsPermissionStatusResponse([new("ads_management", "Granted")]));
        public Task<IReadOnlyList<MetaAdsLocationResponse>> SearchTargetingLocationsAsync(MetaAdsConfiguration config, string accessToken, string query, string countryCode, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MetaAdsLocationResponse>>([new("loc_1", "Rio de Janeiro", "city", "BR", "Brazil", "RJ", null, false, true)]);
        public Task<string> UploadAdImageAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
        {
            Uploads++;
            LastUploadFileName = fileName;
            return Task.FromResult($"meta_hash_{fileName}");
        }
        public Task<string> UploadAdVideoAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
        {
            VideoUploads++;
            LastUploadFileName = fileName;
            if (ThrowVideoUploadTimeout)
            {
                throw new TaskCanceledException("timeout");
            }

            return Task.FromResult($"video_id_{fileName}");
        }
        public Task<IReadOnlyList<MetaAdsPixelResponse>> ListPixelsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MetaAdsPixelResponse>>([]);
        public Task<IReadOnlyList<MetaAdsPageResponse>> ListPagesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdAccountDto> GetAdAccountAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCreativeDto>> GetAdCreativesAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsResourceStatusDto?> GetResourceStatusAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ResourceExistsAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateCampaignAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCampaignCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteCampaignAsync(MetaAdsConfiguration config, string accessToken, string campaignId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdSetAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdSetCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdSetAsync(MetaAdsConfiguration config, string accessToken, string adSetId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCreativeCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateDiagnosticAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsDiagnosticCreativeCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string creativeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdAsync(MetaAdsConfiguration config, string accessToken, string adId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Resolver : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken) => Task.FromResult(new ResolvedConfigurationValue(chave switch
        {
            "PublicBaseUrl" => "https://example.com",
            "DefaultCountryCode" => "BR",
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
