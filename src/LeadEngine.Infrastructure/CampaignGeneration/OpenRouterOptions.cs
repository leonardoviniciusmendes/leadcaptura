namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class OpenRouterOptions
{
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;
    public double Temperature { get; set; } = 0.3;
}
