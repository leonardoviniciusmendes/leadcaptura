using System.Security.Cryptography;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class CreativeAssetService(
    ICampanhaRepository campanhaRepository,
    ICreativeAssetRepository assetRepository,
    ICreativeAssetAnalysisProvider analysisProvider,
    IMetaAdsImagemRepository metaAdsImagemRepository,
    IVideoProcessingService videoProcessingService,
    ILogger<CreativeAssetService> logger,
    IOptions<CreativeAssetOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/gif"] = [".gif"],
        ["image/webp"] = [".webp"],
        ["video/mp4"] = [".mp4"],
        ["video/webm"] = [".webm"]
    };

    public async Task<CreativeAssetUploadResponse> UploadAsync(Guid campaignId, IReadOnlyList<CreativeAssetUploadItem> files, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new ArgumentException("Envie pelo menos uma imagem.");
        }

        var campanha = await campanhaRepository.ObterPorIdAsync(campaignId, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");

        var uploaded = new List<CreativeAssetResponse>();
        foreach (var file in files)
        {
            var validation = await ValidateAsync(file, cancellationToken);
            var id = Guid.NewGuid();
            var safeName = Path.GetFileName(file.FileName);
            var relativePath = Path.Combine("creative-assets", campanha.Id.ToString("N"), $"{id:N}{validation.Extension}").Replace('\\', '/');
            var fullPath = ResolveStoragePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using (var output = File.Create(fullPath))
            {
                file.Content.Position = 0;
                await file.Content.CopyToAsync(output, cancellationToken);
            }

            var width = validation.Width;
            var height = validation.Height;
            var durationSeconds = validation.DurationSeconds;
            string? thumbnailPath = null;
            if (validation.MediaType == CreativeAssetMediaType.Video)
            {
                var metadata = await videoProcessingService.ProbeAsync(fullPath, cancellationToken);
                if (metadata is not null)
                {
                    width = metadata.Width;
                    height = metadata.Height;
                    durationSeconds = metadata.DurationSeconds;
                }

                thumbnailPath = Path.Combine("creative-assets", campanha.Id.ToString("N"), $"{id:N}-poster.png").Replace('\\', '/');
                try
                {
                    var posterFullPath = ResolveStoragePath(thumbnailPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(posterFullPath)!);
                    thumbnailPath = await videoProcessingService.ExtractPosterAsync(fullPath, posterFullPath, durationSeconds ?? validation.DurationSeconds ?? 1, cancellationToken) is null
                        ? null
                        : thumbnailPath;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Video poster extraction failed. AssetId={AssetId}", id);
                    thumbnailPath = null;
                }
            }

            var asset = new CreativeAsset
            {
                Id = id,
                CampaignId = campanha.Id,
                MediaType = validation.MediaType,
                FileName = safeName,
                StoragePath = relativePath,
                ThumbnailPath = thumbnailPath,
                MimeType = validation.MimeType,
                Width = width,
                Height = height,
                DurationSeconds = durationSeconds,
                FileSize = file.Length,
                IsSelected = false,
                CreatedAt = DateTime.UtcNow
            };
            await assetRepository.AdicionarAsync(asset, cancellationToken);
            uploaded.Add(ToResponse(asset));
        }

        await assetRepository.SalvarAsync(cancellationToken);
        return new CreativeAssetUploadResponse(uploaded, $"{uploaded.Count} imagem(ns) enviada(s). Nenhuma publicacao real foi feita.");
    }

    public async Task<IReadOnlyList<CreativeAssetResponse>> ListAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        if (await campanhaRepository.ObterPorIdAsync(campaignId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("Campanha nao encontrada.");
        }

        var assets = await assetRepository.ListarPorCampanhaAsync(campaignId, cancellationToken);
        return assets
            .Select(ToResponse)
            .OrderByDescending(x => x.RankingScore ?? -1)
            .ThenByDescending(x => x.CreatedAt)
            .ToArray();
    }

    public async Task<CreativeAssetAnalysisResponse> AnalyzeAsync(Guid campaignId, Guid assetId, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPorIdAsync(campaignId, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
        var asset = await assetRepository.ObterPorIdAsync(assetId, cancellationToken)
            ?? throw new KeyNotFoundException("Imagem nao encontrada.");
        if (asset.CampaignId != campaignId)
        {
            throw new KeyNotFoundException("Imagem nao encontrada para esta campanha.");
        }

        var content = asset.MediaType == CreativeAssetMediaType.Image
            ? await File.ReadAllBytesAsync(ResolveStoragePath(asset.StoragePath), cancellationToken)
            : null;
        var frames = asset.MediaType == CreativeAssetMediaType.Video
            ? await VideoFramesAsync(asset, cancellationToken)
            : [];
        var briefing = CampanhaMapping.ToBriefing(campanha);
        var result = await analysisProvider.AnalyzeAsync(new CreativeAssetAnalysisProviderRequest(
            asset.Id,
            campanha.Segment?.Name,
            briefing.BusinessDescription,
            briefing.ProductOrService,
            briefing.TargetAudience,
            briefing.CampaignGoal,
            briefing.Offer,
            FormatLocation(briefing.Location),
            briefing.BrandTone,
            briefing.Restrictions.ToArray(),
            asset.FileName,
            asset.MediaType.ToString(),
            asset.MimeType,
            asset.Width,
            asset.Height,
            asset.DurationSeconds,
            content,
            frames), cancellationToken);

        var parsed = ParseAnalysis(result.RawJson);
        if (parsed.Scores.OriginalScale == 10)
        {
            logger.LogInformation(
                "CreativeAnalysis score scale normalized. AssetId={AssetId} OriginalScale=0-10 TargetScale=0-100",
                asset.Id);
        }

        var analysis = new CreativeAssetAnalysis
        {
            Id = Guid.NewGuid(),
            CreativeAssetId = asset.Id,
            Provider = string.IsNullOrWhiteSpace(result.Provider) ? "Unknown" : result.Provider,
            Model = string.IsNullOrWhiteSpace(result.Model) ? "Unknown" : result.Model,
            Summary = parsed.Summary,
            VisualQualityScore = parsed.Scores.VisualQualityScore,
            BrandFitScore = parsed.Scores.BrandFitScore,
            TextDensityScore = parsed.Scores.TextDensityScore,
            PlacementRecommendationsJson = JsonSerializer.Serialize(parsed.Placements, JsonOptions),
            RisksJson = JsonSerializer.Serialize(parsed.Risks, JsonOptions),
            SuggestedHeadline = parsed.SuggestedCopy.Headline,
            SuggestedPrimaryText = parsed.SuggestedCopy.PrimaryText,
            SuggestedDescription = parsed.SuggestedCopy.Description,
            SuggestedCta = parsed.SuggestedCopy.Cta,
            RawResponseJson = result.RawJson,
            CreatedAt = DateTime.UtcNow
        };

        await assetRepository.AdicionarAnaliseAsync(analysis, cancellationToken);
        await assetRepository.SalvarAsync(cancellationToken);
        return ToResponse(analysis);
    }

    public async Task<CreativeAssetResponse> SelectAsync(Guid campaignId, Guid assetId, CancellationToken cancellationToken)
    {
        var assets = await assetRepository.ListarPorCampanhaAsync(campaignId, cancellationToken);
        if (assets.Count == 0)
        {
            throw new KeyNotFoundException("Campanha sem imagens.");
        }

        var selected = assets.FirstOrDefault(x => x.Id == assetId)
            ?? throw new KeyNotFoundException("Imagem nao encontrada para esta campanha.");
        foreach (var asset in assets)
        {
            asset.IsSelected = asset.Id == selected.Id;
        }

        await assetRepository.SalvarAsync(cancellationToken);
        return ToResponse(selected);
    }

    public async Task RemoverAsync(Guid campaignId, Guid assetId, CancellationToken cancellationToken)
    {
        if (await campanhaRepository.ObterPorIdAsync(campaignId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("Campanha nao encontrada.");
        }

        var asset = await assetRepository.ObterPorIdAsync(assetId, cancellationToken)
            ?? throw new KeyNotFoundException("Imagem nao encontrada.");
        if (asset.CampaignId != campaignId)
        {
            throw new KeyNotFoundException("Imagem nao encontrada para esta campanha.");
        }

        var path = ResolveStoragePath(asset.StoragePath);
        var thumbnailPath = string.IsNullOrWhiteSpace(asset.ThumbnailPath) ? null : ResolveStoragePath(asset.ThumbnailPath);
        string? contentHash = null;
        if (File.Exists(path))
        {
            var content = await File.ReadAllBytesAsync(path, cancellationToken);
            contentHash = Convert.ToHexString(SHA256.HashData(content));
        }

        assetRepository.Remover(asset);
        if (!string.IsNullOrWhiteSpace(contentHash))
        {
            await metaAdsImagemRepository.RemoverPorConteudoAsync(campaignId, contentHash, "CreativeAsset", cancellationToken);
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        if (!string.IsNullOrWhiteSpace(thumbnailPath) && File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
        }

        await assetRepository.SalvarAsync(cancellationToken);
    }

    public async Task<(byte[] Content, string MimeType, string FileName)> GetContentAsync(Guid campaignId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await assetRepository.ObterPorIdAsync(assetId, cancellationToken)
            ?? throw new KeyNotFoundException("Imagem nao encontrada.");
        if (asset.CampaignId != campaignId)
        {
            throw new KeyNotFoundException("Imagem nao encontrada para esta campanha.");
        }

        return (await File.ReadAllBytesAsync(ResolveStoragePath(asset.StoragePath), cancellationToken), asset.MimeType, asset.FileName);
    }

    public async Task<(byte[] Content, string MimeType, string FileName)> GetThumbnailAsync(Guid campaignId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await assetRepository.ObterPorIdAsync(assetId, cancellationToken)
            ?? throw new KeyNotFoundException("Imagem nao encontrada.");
        if (asset.CampaignId != campaignId || string.IsNullOrWhiteSpace(asset.ThumbnailPath))
        {
            throw new KeyNotFoundException("Thumbnail nao encontrado para esta midia.");
        }

        return (await File.ReadAllBytesAsync(ResolveStoragePath(asset.ThumbnailPath), cancellationToken), "image/png", $"{Path.GetFileNameWithoutExtension(asset.FileName)}-poster.png");
    }

    private async Task<MediaValidationResult> ValidateAsync(CreativeAssetUploadItem file, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new ArgumentException("Nome da midia obrigatorio.");
        }
        if (file.Length == 0)
        {
            throw new ArgumentException("Midia obrigatoria.");
        }
        var maxAnyFileBytes = Math.Max(EffectiveMaxImageBytes(), EffectiveMaxVideoBytes());
        if (file.Length > maxAnyFileBytes)
        {
            throw new ArgumentException($"Midia excede o limite de {maxAnyFileBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Extensao de midia obrigatoria.");
        }

        var probe = await ReadProbeAsync(file.Content, cancellationToken);
        var image = ImageHeaderReader.Detect(probe);
        if (image is not null)
        {
            return ValidateImage(file, extension, image);
        }

        var video = VideoHeaderReader.Detect(probe);
        if (video is not null)
        {
            return ValidateVideo(file, extension, video);
        }

        throw new ArgumentException("MIME real da midia nao foi reconhecido.");
    }

    private MediaValidationResult ValidateImage(CreativeAssetUploadItem file, string extension, ImageHeader detected)
    {
        if (file.Length > EffectiveMaxImageBytes())
        {
            throw new ArgumentException($"Imagem excede o limite de {EffectiveMaxImageBytes() / 1024 / 1024} MB.");
        }
        if (!AllowedExtensions.TryGetValue(detected.MimeType, out var extensions) || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Extensao da midia nao corresponde ao formato real.");
        }
        if (!string.IsNullOrWhiteSpace(file.ContentType) && !string.Equals(file.ContentType, detected.MimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Content-Type informado nao corresponde ao MIME real da midia.");
        }
        if (detected.Width <= 0 || detected.Height <= 0 || detected.Width > options.Value.MaxWidth || detected.Height > options.Value.MaxHeight)
        {
            throw new ArgumentException("Dimensoes da imagem fora dos limites aceitos.");
        }

        return new MediaValidationResult(CreativeAssetMediaType.Image, detected.MimeType, extension.ToLowerInvariant(), detected.Width, detected.Height, null);
    }

    private MediaValidationResult ValidateVideo(CreativeAssetUploadItem file, string extension, VideoHeader detected)
    {
        if (file.Length > EffectiveMaxVideoBytes())
        {
            throw new ArgumentException($"Video excede o limite de {EffectiveMaxVideoBytes() / 1024 / 1024} MB.");
        }
        if (!AllowedExtensions.TryGetValue(detected.MimeType, out var extensions) || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Extensao da midia nao corresponde ao formato real.");
        }
        if (!string.IsNullOrWhiteSpace(file.ContentType) && !string.Equals(file.ContentType, detected.MimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Content-Type informado nao corresponde ao MIME real da midia.");
        }
        if (detected.DurationSeconds > EffectiveMaxVideoDurationSeconds())
        {
            throw new ArgumentException($"Video excede a duracao maxima de {EffectiveMaxVideoDurationSeconds()} segundos.");
        }
        if (detected.Width <= 0 || detected.Height <= 0 || detected.Width > options.Value.MaxWidth || detected.Height > options.Value.MaxHeight)
        {
            throw new ArgumentException("Dimensoes do video fora dos limites aceitos.");
        }

        return new MediaValidationResult(CreativeAssetMediaType.Video, detected.MimeType, extension.ToLowerInvariant(), detected.Width, detected.Height, detected.DurationSeconds);
    }

    private static async Task<byte[]> ReadProbeAsync(Stream stream, CancellationToken cancellationToken)
    {
        const int maxProbeBytes = 1024 * 1024;
        stream.Position = 0;
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        while (memory.Length < maxProbeBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maxProbeBytes - (int)memory.Length)), cancellationToken);
            if (read == 0)
            {
                break;
            }
            memory.Write(buffer, 0, read);
        }
        stream.Position = 0;
        return memory.ToArray();
    }

    private async Task<IReadOnlyList<CreativeAssetAnalysisFrame>> VideoFramesAsync(CreativeAsset asset, CancellationToken cancellationToken)
    {
        try
        {
            var frames = await videoProcessingService.ExtractAnalysisFramesAsync(ResolveStoragePath(asset.StoragePath), asset.DurationSeconds ?? 1, cancellationToken);
            if (frames.Count == 0)
            {
                throw new InvalidOperationException("Processamento de video nao esta disponivel neste ambiente.");
            }

            logger.LogInformation(
                "Video analysis frames ready. AssetId={AssetId} Duration={Duration} FrameCount={FrameCount} Timestamps={Timestamps}",
                asset.Id,
                asset.DurationSeconds,
                frames.Count,
                string.Join(",", frames.Select(x => x.OffsetSeconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))));
            return frames;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Processamento de video nao esta disponivel neste ambiente.", ex);
        }
    }

    private long EffectiveMaxImageBytes()
    {
        return options.Value.MaxImageBytes > 0 ? options.Value.MaxImageBytes : options.Value.MaxFileBytes;
    }

    private long EffectiveMaxVideoBytes()
    {
        return options.Value.MaxVideoBytes > 0 ? options.Value.MaxVideoBytes : options.Value.MaxFileBytes;
    }

    private int EffectiveMaxVideoDurationSeconds()
    {
        return options.Value.MaxVideoDurationSeconds > 0 ? options.Value.MaxVideoDurationSeconds : 120;
    }

    private string ResolveStoragePath(string relativePath)
    {
        var root = Path.GetFullPath(options.Value.StorageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de armazenamento invalido.");
        }

        return fullPath;
    }

    private static ParsedAnalysis ParseAnalysis(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            var summary = RequiredString(root, "summary", 2000);
            var detectedText = OptionalString(root, "detectedText", 2000) ?? string.Empty;
            var scores = CreativeAnalysisScoreNormalizer.FromJson(root, RequiredScore, RequiredBool);
            var placements = RequiredStringMap(root, root.TryGetProperty("placementRecommendations", out _) ? "placementRecommendations" : "placements");
            var risks = OptionalStringArray(root, "risks");
            var copy = root.TryGetProperty("suggestedCopy", out var copyElement) && copyElement.ValueKind == JsonValueKind.Object
                ? new CreativeAssetSuggestedCopy(
                    OptionalString(copyElement, "headline", 120),
                    OptionalString(copyElement, "primaryText", 500),
                    OptionalString(copyElement, "description", 300),
                    OptionalString(copyElement, "cta", 40))
                : throw new ArgumentException("Resposta IA sem suggestedCopy valido.");
            return new ParsedAnalysis(summary, detectedText, scores, placements, risks, copy);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Resposta IA nao contem JSON valido.", ex);
        }
    }

    private static string RequiredString(JsonElement root, string property, int maxLength)
    {
        var value = OptionalString(root, property, maxLength);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Resposta IA sem {property} valido.")
            : value;
    }

    private static string? OptionalString(JsonElement root, string property, int maxLength)
    {
        return root.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString()?.Trim() is { Length: > 0 } value ? value[..Math.Min(value.Length, maxLength)] : null
            : null;
    }

    private static int RequiredScore(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var item) || !item.TryGetInt32(out var value) || value < 0 || value > 100)
        {
            throw new ArgumentException($"Resposta IA sem {property} entre 0 e 100.");
        }

        return value;
    }

    private static int? OptionalScore(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var item) && item.TryGetInt32(out var value) && value is >= 0 and <= 100
            ? value
            : null;
    }

    private static bool RequiredBool(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new ArgumentException($"Resposta IA sem {property} booleano.");
        }

        return item.GetBoolean();
    }

    private static IReadOnlyDictionary<string, string> RequiredStringMap(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var item) || item.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException($"Resposta IA sem {property} valido.");
        }

        var values = item.EnumerateObject()
            .Where(x => x.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(x.Value.GetString()))
            .ToDictionary(x => x.Name, x => x.Value.GetString()!.Trim(), StringComparer.OrdinalIgnoreCase);
        return values.Count == 0 ? throw new ArgumentException($"Resposta IA sem {property} valido.") : values;
    }

    private static IReadOnlyList<string> OptionalStringArray(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var item) || item.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return item.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(x.GetString()))
            .Select(x => x.GetString()!.Trim())
            .ToArray();
    }

    private static CreativeAssetResponse ToResponse(CreativeAsset asset)
    {
        var latest = asset.Analyses.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var analysis = latest is null ? null : ToResponse(latest);
        return new CreativeAssetResponse(
            asset.Id,
            asset.CampaignId,
            asset.MediaType.ToString(),
            asset.FileName,
            asset.StoragePath,
            asset.MimeType,
            asset.Width,
            asset.Height,
            asset.DurationSeconds,
            asset.FileSize,
            $"/api/campanhas/{asset.CampaignId}/creative-assets/{asset.Id}/content",
            string.IsNullOrWhiteSpace(asset.ThumbnailPath) ? null : $"/api/campanhas/{asset.CampaignId}/creative-assets/{asset.Id}/thumbnail",
            asset.IsSelected,
            asset.CreatedAt,
            analysis,
            analysis?.RankingScore);
    }

    private static CreativeAssetAnalysisResponse ToResponse(CreativeAssetAnalysis analysis)
    {
        var placements = JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(analysis.PlacementRecommendationsJson, JsonOptions)
            ?? new Dictionary<string, string>();
        var risks = JsonSerializer.Deserialize<IReadOnlyList<string>>(analysis.RisksJson, JsonOptions) ?? [];
        var extra = AnalysisExtra.FromRawJson(analysis);
        var ranking = CreativeAnalysisScoreNormalizer.RankingScore(extra.Scores);
        return new CreativeAssetAnalysisResponse(
            analysis.Id,
            analysis.CreativeAssetId,
            analysis.Provider,
            analysis.Model,
            analysis.Summary,
            extra.DetectedText,
            extra.Scores.VisualQualityScore,
            extra.Scores.CampaignFitScore,
            extra.Scores.BrandFitScore,
            extra.Scores.TextDensityScore,
            extra.Scores.MessageConsistencyScore,
            extra.Scores.SemanticMismatch,
            placements,
            risks,
            new CreativeAssetSuggestedCopy(analysis.SuggestedHeadline, analysis.SuggestedPrimaryText, analysis.SuggestedDescription, analysis.SuggestedCta),
            analysis.RawResponseJson,
            analysis.CreatedAt,
            ranking);
    }

    private static string? FormatLocation(CampaignLocationDto? location)
    {
        return location is null ? null : string.Join(" / ", new[] { location.Region, location.City, location.State }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private sealed record MediaValidationResult(CreativeAssetMediaType MediaType, string MimeType, string Extension, int Width, int Height, double? DurationSeconds);

    private sealed record ParsedAnalysis(string Summary, string DetectedText, CreativeAnalysisScores Scores, IReadOnlyDictionary<string, string> Placements, IReadOnlyList<string> Risks, CreativeAssetSuggestedCopy SuggestedCopy);

    private sealed record AnalysisExtra(string DetectedText, CreativeAnalysisScores Scores)
    {
        public static AnalysisExtra FromRawJson(CreativeAssetAnalysis analysis)
        {
            try
            {
                using var doc = JsonDocument.Parse(analysis.RawResponseJson);
                var root = doc.RootElement;
                return new AnalysisExtra(
                    OptionalString(root, "detectedText", 2000) ?? string.Empty,
                    CreativeAnalysisScoreNormalizer.FromJson(root, analysis.VisualQualityScore, analysis.BrandFitScore, analysis.BrandFitScore));
            }
            catch (JsonException)
            {
                return new AnalysisExtra(
                    string.Empty,
                    new CreativeAnalysisScores(analysis.VisualQualityScore, analysis.BrandFitScore, analysis.BrandFitScore, analysis.TextDensityScore, analysis.BrandFitScore, false, false, null, 100));
            }
        }
    }
}
