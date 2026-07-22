namespace LeadEngine.Domain.Entities;

public class OrigemLead
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? Gclid { get; set; }
    public string? Gbraid { get; set; }
    public string? Wbraid { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public string? CampaignId { get; set; }
    public string? AdGroupId { get; set; }
    public string? AdId { get; set; }
    public string? Keyword { get; set; }
    public string? MatchType { get; set; }
    public string? Device { get; set; }
    public string? LandingPage { get; set; }
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public string? IpHash { get; set; }
    public Lead Lead { get; set; } = null!;
}
