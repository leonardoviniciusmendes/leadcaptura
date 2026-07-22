namespace LeadEngine.Infrastructure.Integrations;

public sealed class IntegracaoLeadsOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "/api/leads";
    public string ApiKey { get; set; } = string.Empty;
}
