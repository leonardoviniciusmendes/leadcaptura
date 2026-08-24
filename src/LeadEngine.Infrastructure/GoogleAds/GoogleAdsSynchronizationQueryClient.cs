using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsSynchronizationQueryClient(
    GoogleAdsGaqlClient gaqlClient,
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver,
    GoogleAdsExceptionFormatter exceptionFormatter) : IGoogleAdsSynchronizationQueryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsRemoteStatusSnapshot> GetRemoteStatusAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
    {
        var campaign = resources.FirstOrDefault(x => x.TipoRecurso == "Campaign")?.ResourceName;
        if (string.IsNullOrWhiteSpace(campaign))
        {
            return new GoogleAdsRemoteStatusSnapshot("", null, null, null, null, null, null, [], [], null, ["Campaign resource name ausente."], [], null);
        }
        var (requestId, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, GoogleAdsGaqlQueries.CampaignStatus(campaign), cancellationToken);
        using (body)
        {
            var found = body.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0;
            var row = found ? results[0] : default;
            var campaignElement = found && row.TryGetProperty("campaign", out var c) ? c : default;
            var status = campaignElement.ValueKind == JsonValueKind.Object && campaignElement.TryGetProperty("status", out var statusValue) ? statusValue.GetString() : null;
            var name = campaignElement.ValueKind == JsonValueKind.Object && campaignElement.TryGetProperty("name", out var nameValue) ? nameValue.GetString() : null;
            return found
                ? new GoogleAdsRemoteStatusSnapshot(campaign, name, status, null, null, null, null, [], [], null, [], [], requestId)
                : new GoogleAdsRemoteStatusSnapshot(campaign, null, null, null, null, null, null, [], [], null, [campaign], [], requestId);
        }
    }

    public async Task SetCampaignStatusAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, string status, CancellationToken cancellationToken)
    {
        var normalizedStatus = status.Equals("ENABLED", StringComparison.OrdinalIgnoreCase) ? "ENABLED" : "PAUSED";
        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var body = new
        {
            mutateOperations = new object[]
            {
                new
                {
                    campaignOperation = new
                    {
                        update = new
                        {
                            resourceName = campaignResourceName,
                            status = normalizedStatus
                        },
                        updateMask = "status"
                    }
                }
            },
            partialFailure = false,
            validateOnly = false
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:mutate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GoogleAdsDiagnosticException(exceptionFormatter.FromRestError(text, requestId, ((int)response.StatusCode).ToString(), "Nao foi possivel alterar status remoto da campanha Google Ads."));
        }
    }

    public async Task<string?> SetResourceStatusesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, string status, CancellationToken cancellationToken)
    {
        var normalizedStatus = status.Equals("ENABLED", StringComparison.OrdinalIgnoreCase) ? "ENABLED" : "PAUSED";
        var operations = resources
            .OrderBy(ActivationOrder)
            .Select(resource => StatusOperation(resource, normalizedStatus))
            .Where(x => x is not null)
            .ToArray();
        if (operations.Length == 0)
        {
            throw new InvalidOperationException("Nenhum recurso Google Ads elegivel para ativacao.");
        }

        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var loginCustomerId = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", cancellationToken)).Value;
        var body = new
        {
            mutateOperations = operations,
            partialFailure = false,
            validateOnly = false
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:mutate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        if (!string.IsNullOrWhiteSpace(loginCustomerId))
        {
            request.Headers.TryAddWithoutValidation("login-customer-id", new string(loginCustomerId.Where(char.IsDigit).ToArray()));
        }

        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, cancellationToken);
        var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GoogleAdsDiagnosticException(exceptionFormatter.FromRestError(text, requestId, ((int)response.StatusCode).ToString(), "Nao foi possivel alterar status remoto dos recursos Google Ads."));
        }

        return requestId;
    }

    private static int ActivationOrder(GoogleAdsPublishedResourceDto resource) => resource.TipoRecurso switch
    {
        "AdGroup" => 0,
        "Keyword" => 1,
        "ResponsiveSearchAd" => 2,
        "Campaign" => 3,
        _ => 10
    };

    private static object? StatusOperation(GoogleAdsPublishedResourceDto resource, string status) => resource.TipoRecurso switch
    {
        "AdGroup" => new
        {
            adGroupOperation = new
            {
                update = new { resourceName = resource.ResourceName, status },
                updateMask = "status"
            }
        },
        "Keyword" => new
        {
            adGroupCriterionOperation = new
            {
                update = new { resourceName = resource.ResourceName, status },
                updateMask = "status"
            }
        },
        "ResponsiveSearchAd" => new
        {
            adGroupAdOperation = new
            {
                update = new { resourceName = resource.ResourceName, status },
                updateMask = "status"
            }
        },
        "Campaign" => new
        {
            campaignOperation = new
            {
                update = new { resourceName = resource.ResourceName, status },
                updateMask = "status"
            }
        },
        _ => null
    };
}
