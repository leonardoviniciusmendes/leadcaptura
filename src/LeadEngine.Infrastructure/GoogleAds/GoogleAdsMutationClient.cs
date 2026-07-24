using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Ads.GoogleAds.Lib;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsMutationClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver,
    IGoogleAdsErrorTranslator errorTranslator) : IGoogleAdsMutationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsMutationResult> MutateAsync(string customerId, string accessToken, string developerToken, GoogleAdsOperationPlan plan, bool validateOnly, CancellationToken cancellationToken)
    {
        _ = typeof(GoogleAdsClient);
        try
        {
            var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v19";
            var body = new
            {
                mutateOperations = plan.Operations.Select(x => JsonSerializer.Deserialize<object>(x.PayloadJson, JsonOptions)).ToArray(),
                partialFailure = false,
                validateOnly = validateOnly
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{Digits(customerId)}/googleAds:mutate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.TryAddWithoutValidation("developer-token", developerToken);
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
            var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new GoogleAdsMutationResult(false, requestId, [], [errorTranslator.Translate(((int)response.StatusCode).ToString(), "Google Ads rejeitou a operacao.", null, null, null, null, requestId)], false);
            }
            if (validateOnly)
            {
                return new GoogleAdsMutationResult(true, requestId, [], [], false);
            }

            var resources = ParseResources(text, plan);
            return new GoogleAdsMutationResult(true, requestId, resources, [], false);
        }
        catch (Exception ex)
        {
            return new GoogleAdsMutationResult(false, null, [], [errorTranslator.Translate(ex)], false);
        }
    }

    public Task<IReadOnlyList<GoogleAdsPublishedResourceDto>> CheckResourcesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
    {
        return Task.FromResult(resources.Where(x => !string.IsNullOrWhiteSpace(x.ResourceName)).ToArray() as IReadOnlyList<GoogleAdsPublishedResourceDto>);
    }

    private static IReadOnlyList<GoogleAdsPublishedResourceDto> ParseResources(string text, GoogleAdsOperationPlan plan)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (!doc.RootElement.TryGetProperty("mutateOperationResponses", out var responses) || responses.ValueKind != JsonValueKind.Array)
        {
            return [];
        }
        var result = new List<GoogleAdsPublishedResourceDto>();
        var operations = plan.Operations.ToArray();
        var index = 0;
        foreach (var response in responses.EnumerateArray())
        {
            var resourceName = FindResourceName(response);
            if (!string.IsNullOrWhiteSpace(resourceName) && index < operations.Length)
            {
                result.Add(new GoogleAdsPublishedResourceDto(operations[index].TipoRecurso, resourceName, resourceName.Split('/').LastOrDefault(), operations[index].Nome, "PAUSED"));
            }
            index++;
        }
        return result;
    }

    private static string FindResourceName(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("resourceName") && property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString() ?? string.Empty;
                var nested = FindResourceName(property.Value);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        return string.Empty;
    }

    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
}
