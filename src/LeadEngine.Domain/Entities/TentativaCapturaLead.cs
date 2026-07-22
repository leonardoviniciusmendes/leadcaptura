namespace LeadEngine.Domain.Entities;

public class TentativaCapturaLead
{
    public Guid Id { get; set; }
    public Guid? LeadId { get; set; }
    public string WhatsAppNormalizado { get; set; } = string.Empty;
    public bool Duplicado { get; set; }
    public DateTime CriadoEm { get; set; }
    public string? LandingPage { get; set; }
    public string? UtmCampaign { get; set; }
    public string? Gclid { get; set; }
    public Lead? Lead { get; set; }
}
