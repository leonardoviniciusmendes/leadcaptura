namespace LeadEngine.Application.DTOs;

public sealed record CreativeAssetUploadItem(string FileName, string ContentType, Stream Content, long Length)
{
    public CreativeAssetUploadItem(string fileName, string contentType, byte[] content)
        : this(fileName, contentType, new MemoryStream(content), content.LongLength)
    {
    }
}

public sealed record CreativeAssetUploadResponse(
    IReadOnlyList<CreativeAssetResponse> Assets,
    string Mensagem);

public sealed record CreativeAssetResponse(
    Guid Id,
    Guid CampaignId,
    string MediaType,
    string FileName,
    string StoragePath,
    string MimeType,
    int Width,
    int Height,
    double? DurationSeconds,
    long FileSize,
    string ContentUrl,
    string? ThumbnailUrl,
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
    Guid AssetId,
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
    string MediaType,
    string MimeType,
    int Width,
    int Height,
    double? DurationSeconds,
    byte[]? Content,
    IReadOnlyList<CreativeAssetAnalysisFrame> Frames);

public sealed record CreativeAssetAnalysisFrame(
    string Label,
    double OffsetSeconds,
    string MimeType,
    byte[] Content);

public sealed record CreativeAssetAnalysisProviderResult(
    string Provider,
    string Model,
    string RawJson);
