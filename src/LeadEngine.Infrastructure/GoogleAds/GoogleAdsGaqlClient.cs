using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsGaqlClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver,
    GoogleAdsExceptionFormatter exceptionFormatter)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(string? RequestId, JsonDocument Body)> SearchAsync(string customerId, string accessToken, string developerToken, string query, CancellationToken cancellationToken)
    {
        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var loginCustomerId = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", cancellationToken)).Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        if (!string.IsNullOrWhiteSpace(loginCustomerId))
        {
            request.Headers.TryAddWithoutValidation("login-customer-id", new string(loginCustomerId.Where(char.IsDigit).ToArray()));
        }
        request.Content = new StringContent(JsonSerializer.Serialize(new { query }, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var diagnostic = exceptionFormatter.FromRestError(doc.RootElement.GetRawText(), requestId, ((int)response.StatusCode).ToString(), "Consulta Google Ads falhou.");
            doc.Dispose();
            throw new GoogleAdsDiagnosticException(diagnostic);
        }
        return (requestId, doc);
    }
}
