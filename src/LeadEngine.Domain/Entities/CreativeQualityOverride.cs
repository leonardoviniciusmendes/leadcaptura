namespace LeadEngine.Domain.Entities;

public sealed class CreativeQualityOverride
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CreativeAssetId { get; set; }
    public Guid? CreativeAssetAnalysisId { get; set; }
    public int? RankingScore { get; set; }
    public bool SemanticMismatch { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Campanha Campaign { get; set; } = null!;
    public CreativeAsset CreativeAsset { get; set; } = null!;
    public CreativeAssetAnalysis? CreativeAssetAnalysis { get; set; }
}
