using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed record CampaignPublicUrlBuildResult(
    string? PublicBaseUrl,
    string? Slug,
    string? PersistedUrl,
    string? Url,
    bool Valida,
    string? MotivoFalha);

public sealed class CampaignPublicUrlBuilder(IConfigurationResolver resolver)
{
    public async Task<CampaignPublicUrlBuildResult> BuildAsync(string? slug, string? persistedUrl, CancellationToken cancellationToken)
    {
        var publicBaseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.Application, "PublicBaseUrl", cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            var redirectUri = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "RedirectUri", cancellationToken)).Value;
            publicBaseUrl = InferBaseUrlFromRedirectUri(redirectUri);
        }

        return Build(slug, publicBaseUrl, persistedUrl);
    }

    public async Task<string> BuildRequiredAsync(string slug, string? persistedUrl, CancellationToken cancellationToken)
    {
        var result = await BuildAsync(slug, persistedUrl, cancellationToken);
        if (!result.Valida || string.IsNullOrWhiteSpace(result.Url))
        {
            throw new InvalidOperationException(result.MotivoFalha ?? "URL publica da landing invalida.");
        }

        return result.Url;
    }

    public static CampaignPublicUrlBuildResult Build(string? slug, string? publicBaseUrl, string? persistedUrl = null)
    {
        var cleanSlug = slug?.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(cleanSlug))
        {
            return new CampaignPublicUrlBuildResult(publicBaseUrl, slug, persistedUrl, null, false, "Slug publico ausente.");
        }

        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            var cleanBase = publicBaseUrl.Trim().TrimEnd('/');
            if (!Uri.TryCreate(cleanBase, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps && !baseUri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                return new CampaignPublicUrlBuildResult(publicBaseUrl, cleanSlug, persistedUrl, null, false, "Application.PublicBaseUrl nao e uma URL publica valida.");
            }

            return new CampaignPublicUrlBuildResult(publicBaseUrl, cleanSlug, persistedUrl, $"{cleanBase}/lp/{Uri.EscapeDataString(cleanSlug)}", true, null);
        }

        if (IsPublicAbsoluteUrl(persistedUrl))
        {
            return new CampaignPublicUrlBuildResult(publicBaseUrl, cleanSlug, persistedUrl, persistedUrl!.Trim(), true, null);
        }

        var reason = string.IsNullOrWhiteSpace(persistedUrl)
            ? "Application.PublicBaseUrl vazio e UrlPublica nao persistida."
            : "Application.PublicBaseUrl vazio e UrlPublica persistida nao e absoluta.";
        return new CampaignPublicUrlBuildResult(publicBaseUrl, cleanSlug, persistedUrl, persistedUrl, false, reason);
    }

    public static string? InferBaseUrlFromRedirectUri(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri) || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        const string configuracoes = "/configuracoes";
        if (path.EndsWith(configuracoes, StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^configuracoes.Length].TrimEnd('/');
        }

        return $"{uri.Scheme}://{uri.Authority}{path}";
    }

    private static bool IsPublicAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }
}
