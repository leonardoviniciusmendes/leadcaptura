using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class CreativeAssetService(
    ICampanhaRepository campanhaRepository,
    ICreativeAssetRepository assetRepository,
    ICreativeAssetAnalysisProvider analysisProvider,
    IOptions<CreativeAssetOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/gif"] = [".gif"],
        ["image/webp"] = [".webp"]
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
            var validation = Validate(file);
            var id = Guid.NewGuid();
            var safeName = Path.GetFileName(file.FileName);
            var relativePath = Path.Combine("creative-assets", campanha.Id.ToString("N"), $"{id:N}{validation.Extension}").Replace('\\', '/');
            var fullPath = ResolveStoragePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, file.Content, cancellationToken);

            var asset = new CreativeAsset
            {
                Id = id,
                CampaignId = campanha.Id,
                FileName = safeName,
                StoragePath = relativePath,
                MimeType = validation.MimeType,
                Width = validation.Width,
                Height = validation.Height,
                FileSize = file.Content.LongLength,
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

        var content = await File.ReadAllBytesAsync(ResolveStoragePath(asset.StoragePath), cancellationToken);
        var briefing = CampanhaMapping.ToBriefing(campanha);
        var result = await analysisProvider.AnalyzeAsync(new CreativeAssetAnalysisProviderRequest(
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
            asset.MimeType,
            asset.Width,
            asset.Height,
            content), cancellationToken);

        var parsed = ParseAnalysis(result.RawJson);
        var analysis = new CreativeAssetAnalysis
        {
            Id = Guid.NewGuid(),
            CreativeAssetId = asset.Id,
            Provider = string.IsNullOrWhiteSpace(result.Provider) ? "Unknown" : result.Provider,
            Model = string.IsNullOrWhiteSpace(result.Model) ? "Unknown" : result.Model,
            Summary = parsed.Summary,
            VisualQualityScore = parsed.VisualQualityScore,
            BrandFitScore = parsed.BrandFitScore,
            TextDensityScore = parsed.TextDensityScore,
            PlacementRecommendationsJson = JsonSerializer.Serialize(parsed.Placements, JsonOptions),
            RisksJson = JsonSerializer.Serialize(parsed.Risks, JsonOptions),
            SuggestedHeadline = parsed.SuggestedCopy.Headline,
            SuggestedPrimaryText = parsed.SuggestedCopy.PrimaryText,
            SuggestedDescription = parsed.SuggestedCopy.Description,
            SuggestedCta = parsed.SuggestedCopy.Cta,
            RawResponseJson = NormalizeJson(result.RawJson),
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

    private ImageValidationResult Validate(CreativeAssetUploadItem file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new ArgumentException("Nome da imagem obrigatorio.");
        }
        if (file.Content.Length == 0)
        {
            throw new ArgumentException("Imagem obrigatoria.");
        }
        if (file.Content.LongLength > options.Value.MaxFileBytes)
        {
            throw new ArgumentException($"Imagem excede o limite de {options.Value.MaxFileBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Extensao de imagem obrigatoria.");
        }

        var detected = ImageHeaderReader.Detect(file.Content)
            ?? throw new ArgumentException("MIME real da imagem nao foi reconhecido.");
        if (!AllowedExtensions.TryGetValue(detected.MimeType, out var extensions) || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Extensao da imagem nao corresponde ao formato real.");
        }
        if (!string.IsNullOrWhiteSpace(file.ContentType) && !string.Equals(file.ContentType, detected.MimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Content-Type informado nao corresponde ao MIME real da imagem.");
        }
        if (detected.Width <= 0 || detected.Height <= 0 || detected.Width > options.Value.MaxWidth || detected.Height > options.Value.MaxHeight)
        {
            throw new ArgumentException("Dimensoes da imagem fora dos limites aceitos.");
        }

        return new ImageValidationResult(detected.MimeType, extension.ToLowerInvariant(), detected.Width, detected.Height);
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
            var visual = RequiredScore(root, "visualQualityScore");
            var campaign = RequiredScore(root, "campaignFitScore");
            var brand = RequiredScore(root, "brandFitScore");
            var density = RequiredScore(root, "textDensityScore");
            var consistency = RequiredScore(root, "messageConsistencyScore");
            var semanticMismatch = RequiredBool(root, "semanticMismatch");
            var placements = RequiredStringMap(root, root.TryGetProperty("placementRecommendations", out _) ? "placementRecommendations" : "placements");
            var risks = OptionalStringArray(root, "risks");
            var copy = root.TryGetProperty("suggestedCopy", out var copyElement) && copyElement.ValueKind == JsonValueKind.Object
                ? new CreativeAssetSuggestedCopy(
                    OptionalString(copyElement, "headline", 120),
                    OptionalString(copyElement, "primaryText", 500),
                    OptionalString(copyElement, "description", 300),
                    OptionalString(copyElement, "cta", 40))
                : throw new ArgumentException("Resposta IA sem suggestedCopy valido.");
            return new ParsedAnalysis(summary, detectedText, visual, campaign, brand, density, consistency, semanticMismatch, placements, risks, copy);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Resposta IA nao contem JSON valido.", ex);
        }
    }

    private static string NormalizeJson(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        return JsonSerializer.Serialize(doc.RootElement, JsonOptions);
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
            asset.FileName,
            asset.StoragePath,
            asset.MimeType,
            asset.Width,
            asset.Height,
            asset.FileSize,
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
        var extra = AnalysisExtra.FromRawJson(analysis.RawResponseJson, analysis.BrandFitScore);
        var ranking = RankingScore(analysis.VisualQualityScore, extra.CampaignFitScore, analysis.BrandFitScore, extra.MessageConsistencyScore, extra.SemanticMismatch);
        return new CreativeAssetAnalysisResponse(
            analysis.Id,
            analysis.CreativeAssetId,
            analysis.Provider,
            analysis.Model,
            analysis.Summary,
            extra.DetectedText,
            analysis.VisualQualityScore,
            extra.CampaignFitScore,
            analysis.BrandFitScore,
            analysis.TextDensityScore,
            extra.MessageConsistencyScore,
            extra.SemanticMismatch,
            placements,
            risks,
            new CreativeAssetSuggestedCopy(analysis.SuggestedHeadline, analysis.SuggestedPrimaryText, analysis.SuggestedDescription, analysis.SuggestedCta),
            analysis.RawResponseJson,
            analysis.CreatedAt,
            ranking);
    }

    private static int RankingScore(int visualQualityScore, int campaignFitScore, int brandFitScore, int messageConsistencyScore, bool semanticMismatch)
    {
        var score = (int)Math.Round(
            campaignFitScore * 0.35
            + messageConsistencyScore * 0.30
            + brandFitScore * 0.20
            + visualQualityScore * 0.15,
            MidpointRounding.AwayFromZero);
        return semanticMismatch ? Math.Min(score, 35) : score;
    }

    private static string? FormatLocation(CampaignLocationDto? location)
    {
        return location is null ? null : string.Join(" / ", new[] { location.Region, location.City, location.State }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private sealed record ImageValidationResult(string MimeType, string Extension, int Width, int Height);
    private sealed record ParsedAnalysis(string Summary, string DetectedText, int VisualQualityScore, int CampaignFitScore, int BrandFitScore, int TextDensityScore, int MessageConsistencyScore, bool SemanticMismatch, IReadOnlyDictionary<string, string> Placements, IReadOnlyList<string> Risks, CreativeAssetSuggestedCopy SuggestedCopy);

    private sealed record AnalysisExtra(string DetectedText, int CampaignFitScore, int MessageConsistencyScore, bool SemanticMismatch)
    {
        public static AnalysisExtra FromRawJson(string rawJson, int fallbackScore)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;
                return new AnalysisExtra(
                    OptionalString(root, "detectedText", 2000) ?? string.Empty,
                    OptionalScore(root, "campaignFitScore") ?? fallbackScore,
                    OptionalScore(root, "messageConsistencyScore") ?? fallbackScore,
                    root.TryGetProperty("semanticMismatch", out var mismatch) && mismatch.ValueKind is JsonValueKind.True or JsonValueKind.False && mismatch.GetBoolean());
            }
            catch (JsonException)
            {
                return new AnalysisExtra(string.Empty, fallbackScore, fallbackScore, false);
            }
        }

        private static int? OptionalScore(JsonElement root, string property)
        {
            return root.TryGetProperty(property, out var item) && item.TryGetInt32(out var value) && value is >= 0 and <= 100
                ? value
                : null;
        }
    }
}
