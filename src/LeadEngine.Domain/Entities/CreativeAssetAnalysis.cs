namespace LeadEngine.Domain.Entities;

public sealed class CreativeAssetAnalysis
{
    public Guid Id { get; set; }
    public Guid CreativeAssetId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int VisualQualityScore { get; set; }
    public int BrandFitScore { get; set; }
    public int TextDensityScore { get; set; }
    public string PlacementRecommendationsJson { get; set; } = "{}";
    public string RisksJson { get; set; } = "[]";
    public string? SuggestedHeadline { get; set; }
    public string? SuggestedPrimaryText { get; set; }
    public string? SuggestedDescription { get; set; }
    public string? SuggestedCta { get; set; }
    public string RawResponseJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public CreativeAsset CreativeAsset { get; set; } = null!;
}
