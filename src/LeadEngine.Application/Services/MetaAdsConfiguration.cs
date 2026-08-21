namespace LeadEngine.Application.Services;

public sealed record MetaAdsConfiguration(
    string? AppId,
    string? AppSecret,
    string RedirectUri,
    string AuthEndpoint,
    string TokenEndpoint,
    string UserInfoEndpoint,
    string GraphApiBaseUrl,
    string GraphApiVersion,
    string Scopes)
{
    public bool OAuthConfigurado =>
        !string.IsNullOrWhiteSpace(AppId)
        && !string.IsNullOrWhiteSpace(AppSecret)
        && !string.IsNullOrWhiteSpace(RedirectUri)
        && !string.IsNullOrWhiteSpace(AuthEndpoint)
        && !string.IsNullOrWhiteSpace(TokenEndpoint)
        && !string.IsNullOrWhiteSpace(UserInfoEndpoint);
}
