using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsRestMutateTransport(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(bool Success, string? RequestId, string Body, string? ErrorCode)> SendAsync(
        string customerId,
        string accessToken,
        string developerToken,
        GoogleAdsOperationPlan plan,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var timeoutSeconds = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiTimeoutSeconds", cancellationToken)).Value, out var timeout) ? timeout : 60;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 300)));

        var body = JsonSerializer.Deserialize<JsonElement>(new GoogleAdsTypedOperationFactory().ToGoogleAdsJson(plan), JsonOptions);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(body, JsonOptions));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:mutate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        request.Content = new StringContent(WithExecutionFlags(doc.RootElement, validateOnly), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, timeoutCts.Token);
        var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
        var text = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        return (response.IsSuccessStatusCode, requestId, text, response.IsSuccessStatusCode ? null : ((int)response.StatusCode).ToString());
    }

    private static string WithExecutionFlags(JsonElement root, bool validateOnly)
    {
        var operations = root.GetProperty("mutateOperations");
        return JsonSerializer.Serialize(new
        {
            mutateOperations = JsonSerializer.Deserialize<object>(operations.GetRawText(), JsonOptions),
            partialFailure = false,
            validateOnly
        }, JsonOptions);
    }
}
