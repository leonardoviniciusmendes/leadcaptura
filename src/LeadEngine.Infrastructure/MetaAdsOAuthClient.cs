using System.Net.Http.Headers;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure;

public sealed class MetaAdsOAuthClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver) : IMetaAdsOAuthClient
{
    public string BuildAuthorizationUrl(MetaAdsConfiguration config, string state)
    {
        return AddQueryString(config.AuthEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = config.AppId,
            ["redirect_uri"] = config.RedirectUri,
            ["state"] = state,
            ["response_type"] = "code",
            ["scope"] = config.Scopes
        });
    }

    public async Task<MetaAdsTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var url = AddQueryString(config.TokenEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = config.AppId,
            ["client_secret"] = config.AppSecret,
            ["redirect_uri"] = config.RedirectUri,
            ["code"] = code
        });

        using var response = await httpClientFactory.CreateClient("metaads").GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(FriendlyMetaError(text, "Falha ao trocar codigo OAuth Meta por token."));
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var accessToken = ReadString(root, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Resposta OAuth Meta sem access token.");
        }

        return new MetaAdsTokenResult(
            accessToken,
            ReadString(root, "token_type"),
            ReadInt(root, "expires_in"));
    }

    public async Task<MetaAdsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, AddQueryString(config.UserInfoEndpoint, new Dictionary<string, string?> { ["fields"] = "id,name" }));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(FriendlyMetaError(text, "Falha ao recuperar usuario Meta conectado."));
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var id = ReadString(json.RootElement, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Resposta Meta sem identificador do usuario.");
        }

        return new MetaAdsUserInfo(id, ReadString(json.RootElement, "name"));
    }

    private async Task<MetaAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new MetaAdsConfiguration(
            await Value("AppId", cancellationToken),
            await Value("AppSecret", cancellationToken),
            await Value("RedirectUri", cancellationToken) ?? string.Empty,
            await Value("AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value("TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value("UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value("GraphApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value("GraphApiVersion", cancellationToken) ?? string.Empty,
            await Value("Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? ReadInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : null;
    }

    private static string FriendlyMetaError(string body, string fallback)
    {
        try
        {
            using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var codeValue) ? codeValue.ToString() : null;
                var type = ReadString(error, "type");
                return string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(type)
                    ? fallback
                    : $"Falha OAuth Meta ({type ?? "erro"} {code ?? string.Empty}).";
            }
        }
        catch
        {
            return fallback;
        }

        return fallback;
    }

    private static string AddQueryString(string url, Dictionary<string, string?> values)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return url + separator + string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
    }
}
