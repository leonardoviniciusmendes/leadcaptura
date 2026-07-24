using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsMutationClient(
    IGoogleAdsErrorTranslator errorTranslator,
    GoogleAdsTypedOperationFactory typedOperationFactory,
    GoogleAdsRestMutateTransport transport) : IGoogleAdsMutationClient
{
    public async Task<GoogleAdsMutationResult> MutateAsync(string customerId, string accessToken, string developerToken, GoogleAdsOperationPlan plan, bool validateOnly, CancellationToken cancellationToken)
    {
        try
        {
            _ = typedOperationFactory.Create(plan);
            var result = await transport.SendAsync(customerId, accessToken, developerToken, plan, validateOnly, cancellationToken);
            if (!result.Success)
            {
                return new GoogleAdsMutationResult(false, result.RequestId, [], [errorTranslator.Translate(result.ErrorCode ?? "google_ads_error", "Google Ads rejeitou a operacao.", null, null, null, null, result.RequestId)], false);
            }

            return validateOnly
                ? new GoogleAdsMutationResult(true, result.RequestId, [], [], false)
                : new GoogleAdsMutationResult(true, result.RequestId, ParseResources(result.Body, plan), [], false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new GoogleAdsMutationResult(false, null, [], [errorTranslator.Translate("timeout", "Tempo limite excedido ao chamar Google Ads.", null, null, null, null, null)], false);
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
        if (element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("resourceName") && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString() ?? string.Empty;
            }

            var nested = FindResourceName(property.Value);
            if (!string.IsNullOrWhiteSpace(nested))
            {
                return nested;
            }
        }

        return string.Empty;
    }
}
