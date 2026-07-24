namespace LeadEngine.Application.Services;

public sealed record GoogleAdsConfiguration(
    string? ClientId,
    string? ClientSecret,
    string? DeveloperToken,
    string? LoginCustomerId,
    string RedirectUri,
    string AuthEndpoint,
    string TokenEndpoint,
    string UserInfoEndpoint,
    string ApiBaseUrl,
    string Scopes)
{
    public bool OAuthConfigurado => !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RedirectUri);

    public bool ApiConfigurada => OAuthConfigurado && !string.IsNullOrWhiteSpace(DeveloperToken);
}
