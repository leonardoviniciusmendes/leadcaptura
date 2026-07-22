namespace LeadEngine.Application.DTOs;

public sealed record OrigemLeadDto(
    string? Gclid,
    string? Gbraid,
    string? Wbraid,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmContent,
    string? UtmTerm,
    string? CampaignId,
    string? AdGroupId,
    string? AdId,
    string? Keyword,
    string? MatchType,
    string? Device,
    string? LandingPage,
    string? Referrer,
    string? UserAgent);
