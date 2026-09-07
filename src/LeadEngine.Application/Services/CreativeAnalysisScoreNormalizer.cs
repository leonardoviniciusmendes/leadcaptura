using System.Text.Json;

namespace LeadEngine.Application.Services;

public sealed record CreativeAnalysisScores(
    int VisualQualityScore,
    int CampaignFitScore,
    int BrandFitScore,
    int TextDensityScore,
    int MessageConsistencyScore,
    bool SemanticMismatchOriginal,
    bool SemanticMismatch,
    int? OriginalScale,
    int TargetScale);

public static class CreativeAnalysisScoreNormalizer
{
    public static CreativeAnalysisScores FromJson(JsonElement root, Func<JsonElement, string, int> requiredScore, Func<JsonElement, string, bool> requiredBool)
    {
        var visual = requiredScore(root, "visualQualityScore");
        var campaign = requiredScore(root, "campaignFitScore");
        var brand = requiredScore(root, "brandFitScore");
        var density = requiredScore(root, "textDensityScore");
        var consistency = requiredScore(root, "messageConsistencyScore");
        var semanticMismatchOriginal = requiredBool(root, "semanticMismatch");
        return Normalize(root, visual, campaign, brand, density, consistency, semanticMismatchOriginal);
    }

    public static CreativeAnalysisScores FromJson(JsonElement root, int visualFallback, int brandFallback, int fallbackScore)
    {
        var visual = OptionalScore(root, "visualQualityScore") ?? visualFallback;
        var campaign = OptionalScore(root, "campaignFitScore") ?? fallbackScore;
        var brand = OptionalScore(root, "brandFitScore") ?? brandFallback;
        var density = OptionalScore(root, "textDensityScore") ?? fallbackScore;
        var consistency = OptionalScore(root, "messageConsistencyScore") ?? fallbackScore;
        var semanticMismatchOriginal = root.TryGetProperty("semanticMismatchOriginal", out var originalElement) && originalElement.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? originalElement.GetBoolean()
            : root.TryGetProperty("semanticMismatch", out var mismatchElement) && mismatchElement.ValueKind is JsonValueKind.True or JsonValueKind.False && mismatchElement.GetBoolean();
        return Normalize(root, visual, campaign, brand, density, consistency, semanticMismatchOriginal);
    }

    public static int RankingScore(CreativeAnalysisScores scores)
    {
        var score = (int)Math.Round(
            scores.CampaignFitScore * 0.35
            + scores.MessageConsistencyScore * 0.30
            + scores.BrandFitScore * 0.20
            + scores.VisualQualityScore * 0.15,
            MidpointRounding.AwayFromZero);
        return scores.SemanticMismatch ? Math.Min(score, 35) : score;
    }

    private static CreativeAnalysisScores Normalize(JsonElement root, int visual, int campaign, int brand, int density, int consistency, bool semanticMismatchOriginal)
    {
        var useTenPointScale = IsLikelyTenPointScale(root, [visual, campaign, brand, density, consistency], semanticMismatchOriginal);
        if (useTenPointScale)
        {
            visual *= 10;
            campaign *= 10;
            brand *= 10;
            density *= 10;
            consistency *= 10;
        }

        var semanticMismatch = semanticMismatchOriginal || IsDefensiveSemanticMismatch(campaign, consistency);
        return new CreativeAnalysisScores(
            ClampScore(visual),
            ClampScore(campaign),
            ClampScore(brand),
            ClampScore(density),
            ClampScore(consistency),
            semanticMismatchOriginal,
            semanticMismatch,
            useTenPointScale ? 10 : null,
            100);
    }

    private static bool IsLikelyTenPointScale(JsonElement root, IReadOnlyList<int> scores, bool semanticMismatchOriginal)
    {
        return !semanticMismatchOriginal
            && scores.All(x => x is >= 0 and <= 10)
            && scores.Any(x => x >= 8)
            && HasPositiveEvidence(root)
            && !HasStrongNegativeEvidence(root);
    }

    private static bool HasPositiveEvidence(JsonElement root)
    {
        var summary = Text(root, "summary");
        if (ContainsAny(summary, ["excelente", "perfeito", "perfeitamente", "alinha", "coerente", "adequado", "bom", "recomendado"]))
        {
            return true;
        }

        if (!root.TryGetProperty("placementRecommendations", out var placements) && !root.TryGetProperty("placements", out placements))
        {
            return false;
        }

        return placements.ValueKind == JsonValueKind.Object
            && placements.EnumerateObject().Count(x => string.Equals(x.Value.GetString(), "recommended", StringComparison.OrdinalIgnoreCase)) >= 2;
    }

    private static bool HasStrongNegativeEvidence(JsonElement root)
    {
        var combined = $"{Text(root, "summary")} {Text(root, "detectedText")}";
        if (ContainsAny(combined, ["nao corresponde", "não corresponde", "incompativel", "incompatível", "outro segmento", "corrompido", "sem conteudo", "sem conteúdo"]))
        {
            return true;
        }

        if (!root.TryGetProperty("risks", out var risks) || risks.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return risks.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Any(x => ContainsAny(x.GetString() ?? string.Empty, ["nao corresponde", "não corresponde", "incompativel", "incompatível", "outro segmento"]));
    }

    private static string Text(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> needles)
    {
        return needles.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static int ClampScore(int value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static int? OptionalScore(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var item) && item.TryGetInt32(out var value) && value is >= 0 and <= 100
            ? value
            : null;
    }

    private static bool IsDefensiveSemanticMismatch(int campaignFitScore, int messageConsistencyScore)
    {
        return campaignFitScore <= 15 && messageConsistencyScore <= 15;
    }
}
