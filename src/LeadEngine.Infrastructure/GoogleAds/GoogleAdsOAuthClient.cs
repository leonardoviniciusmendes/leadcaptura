using System.Net.Http.Headers;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsOAuthClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver) : IGoogleAdsOAuthClient
{
    public async Task<GoogleAdsTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var values = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };

        return await RequestTokenAsync(config.TokenEndpoint, values, cancellationToken);
    }

    public async Task<GoogleAdsTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var values = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        return await RequestTokenAsync(config.TokenEndpoint, values, cancellationToken);
    }

    public async Task<GoogleAdsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, config.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new GoogleAdsUserInfo(null, null);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        return new GoogleAdsUserInfo(ReadString(root, "email"), ReadString(root, "name"));
    }

    public async Task<IReadOnlyList<GoogleAdsAccessibleAccount>> ListAccessibleAccountsAsync(string accessToken, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{config.ApiBaseUrl.TrimEnd('/')}/customers:listAccessibleCustomers");
        AddGoogleAdsHeaders(request, config, accessToken);
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Nao foi possivel listar contas Google Ads.");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("resourceNames", out var names) || names.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return names.EnumerateArray()
            .Select(x => x.GetString() ?? string.Empty)
            .Where(x => x.StartsWith("customers/", StringComparison.OrdinalIgnoreCase))
            .Select(x => x["customers/".Length..])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new GoogleAdsAccessibleAccount(x, $"Google Ads {x}"))
            .ToArray();
    }

    public async Task TestConnectionAsync(string accessToken, string customerId, CancellationToken cancellationToken)
    {
        var accounts = await ListAccessibleAccountsAsync(accessToken, cancellationToken);
        var normalized = new string(customerId.Where(char.IsDigit).ToArray());
        if (!accounts.Any(x => new string(x.CustomerId.Where(char.IsDigit).ToArray()) == normalized))
        {
            throw new InvalidOperationException("Conta Google Ads nao encontrada para o token conectado.");
        }
    }

    private async Task<GoogleAdsTokenResult> RequestTokenAsync(string tokenEndpoint, Dictionary<string, string?> values, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => new KeyValuePair<string, string>(x.Key, x.Value!)));
        using var response = await httpClientFactory.CreateClient("googleads").PostAsync(tokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Falha na autenticacao Google Ads.");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var accessToken = ReadString(root, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Resposta OAuth sem access token.");
        }

        return new GoogleAdsTokenResult(
            accessToken,
            ReadString(root, "refresh_token"),
            ReadInt(root, "expires_in") ?? 3600,
            ReadString(root, "scope"),
            ReadString(root, "token_type"));
    }

    private static void AddGoogleAdsHeaders(HttpRequestMessage request, GoogleAdsConfiguration config, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", config.DeveloperToken);
        if (!string.IsNullOrWhiteSpace(config.LoginCustomerId))
        {
            request.Headers.TryAddWithoutValidation("login-customer-id", new string(config.LoginCustomerId.Where(char.IsDigit).ToArray()));
        }
    }

    private async Task<GoogleAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new GoogleAdsConfiguration(
            await Value("ClientId", cancellationToken),
            await Value("ClientSecret", cancellationToken),
            await Value("DeveloperToken", cancellationToken),
            await Value("LoginCustomerId", cancellationToken),
            await Value("RedirectUri", cancellationToken) ?? string.Empty,
            await Value("AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value("TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value("UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value("ApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value("Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? ReadInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : null;
    }
}
