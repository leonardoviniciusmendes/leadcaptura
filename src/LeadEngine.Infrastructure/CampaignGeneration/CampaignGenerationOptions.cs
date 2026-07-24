namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class CampaignGenerationOptions
{
    public string Provider { get; set; } = "Fake";
    public bool FallbackToFake { get; set; }
}
