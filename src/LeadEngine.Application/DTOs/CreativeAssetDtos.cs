namespace LeadEngine.Application.DTOs;

public sealed record CreativeAssetUploadItem(string FileName, string ContentType, byte[] Content);

public sealed record CreativeAssetUploadResponse(
    IReadOnlyList<CreativeAssetResponse> Assets,
    string Mensagem);

public sealed record CreativeAssetResponse(
    Guid Id,
    Guid CampaignId,
    string FileName,
    string StoragePath,
    string MimeType,
    int Width,
    int Height,
    long FileSize,
    bool IsSelected,
    DateTime CreatedAt,
    CreativeAssetAnalysisResponse? LatestAnalysis,
    int? RankingScore);

public sealed record CreativeAssetAnalysisResponse(
    Guid Id,
    Guid CreativeAssetId,
    string Provider,
    string Model,
    string Summary,
    string DetectedText,
    int VisualQualityScore,
    int CampaignFitScore,
    int BrandFitScore,
    int TextDensityScore,
    int MessageConsistencyScore,
    bool SemanticMismatch,
    IReadOnlyDictionary<string, string> Placements,
    IReadOnlyList<string> Risks,
    CreativeAssetSuggestedCopy SuggestedCopy,
    string RawResponseJson,
    DateTime CreatedAt,
    int RankingScore);

public sealed record CreativeAssetSuggestedCopy(
    string? Headline,
    string? PrimaryText,
    string? Description,
    string? Cta);

public sealed record CreativeAssetAnalysisProviderRequest(
    string? Segment,
    string? BusinessDescription,
    string? ProductOrService,
    string? TargetAudience,
    string? CampaignGoal,
    string? Offer,
    string? Location,
    string? BrandTone,
    IReadOnlyList<string> Restrictions,
    string FileName,
    string MimeType,
    int Width,
    int Height,
    byte[] Content);

public sealed record CreativeAssetAnalysisProviderResult(
    string Provider,
    string Model,
    string RawJson);
