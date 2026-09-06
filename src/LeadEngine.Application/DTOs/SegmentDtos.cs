namespace LeadEngine.Application.DTOs;

public sealed record SegmentResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string TemplateKey,
    bool UsesLegacyBriefing,
    string? DefaultCampaignGoal);

public sealed record AdminSegmentResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string TemplateKey,
    string? DefaultConfigJson,
    bool IsActive,
    int CampaignsCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record UpsertSegmentRequest(
    string? Name,
    string? Slug,
    string? Description,
    string? TemplateKey,
    string? DefaultConfigJson,
    bool IsActive);

public sealed record UpdateSegmentStatusRequest(bool IsActive);
