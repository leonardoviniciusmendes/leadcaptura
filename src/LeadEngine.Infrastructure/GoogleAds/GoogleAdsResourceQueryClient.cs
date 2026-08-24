using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsResourceQueryClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver,
    GoogleAdsExceptionFormatter exceptionFormatter) : IGoogleAdsResourceQueryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>> CheckResourcesAsync(
        string customerId,
        string accessToken,
        string developerToken,
        IReadOnlyList<GoogleAdsPublishedResourceDto> resources,
        CancellationToken cancellationToken)
    {
        var validResources = resources.Where(x => !string.IsNullOrWhiteSpace(x.ResourceName)).ToArray();
        var checks = new Dictionary<string, GoogleAdsPublishedResourceCheckDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in validResources.Where(x => QueryKindFor(x) is null))
        {
            checks[resource.ResourceName] = new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, false, false, "Tipo de recurso sem consulta de reconciliacao.");
        }

        foreach (var group in validResources.Where(x => QueryKindFor(x) is not null).GroupBy(QueryKindFor))
        {
            foreach (var check in await CheckBatchAsync(customerId, accessToken, developerToken, group.ToArray(), cancellationToken))
            {
                checks[check.ResourceName] = check;
            }
        }

        return validResources
            .Select(resource => checks.TryGetValue(resource.ResourceName, out var check)
                ? check
                : new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, false, false, "Recurso ausente."))
            .ToArray();
    }

    private async Task<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>> CheckBatchAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
    {
        var query = QueryFor(resources);
        if (query is null)
        {
            return resources.Select(resource => new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, false, false, "Tipo de recurso sem consulta de reconciliacao.")).ToArray();
        }

        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var loginCustomerId = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "LoginCustomerId", cancellationToken)).Value;
        var timeoutSeconds = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiTimeoutSeconds", cancellationToken)).Value, out var timeout) ? timeout : 60;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 300)));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        if (!string.IsNullOrWhiteSpace(loginCustomerId))
        {
            request.Headers.TryAddWithoutValidation("login-customer-id", new string(loginCustomerId.Where(char.IsDigit).ToArray()));
        }

        request.Content = new StringContent(JsonSerializer.Serialize(new { query }, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var requestId = response.Headers.TryGetValues("request-id", out var values) ? values.FirstOrDefault() : null;
            throw new GoogleAdsDiagnosticException(exceptionFormatter.FromRestError(body, requestId, ((int)response.StatusCode).ToString(), "Nao foi possivel consultar o recurso no Google Ads."));
        }

        var found = FoundResourceNames(body);
        return resources
            .Where(resource => found.Contains(resource.ResourceName))
            .Select(resource => new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, true, false, "Recurso encontrado."))
            .ToArray();
    }

    private static string? QueryFor(IReadOnlyList<GoogleAdsPublishedResourceDto> resources)
    {
        var kind = QueryKindFor(resources.First());
        if (kind is null)
        {
            return null;
        }

        var names = string.Join(", ", resources.Select(x => $"'{Escape(x.ResourceName)}'"));
        return kind switch
        {
            "Budget" => $"SELECT campaign_budget.resource_name, campaign_budget.name, campaign_budget.status FROM campaign_budget WHERE campaign_budget.resource_name IN ({names})",
            "Campaign" => $"SELECT campaign.resource_name, campaign.name, campaign.status FROM campaign WHERE campaign.resource_name IN ({names})",
            "CampaignCriterion" => $"SELECT campaign_criterion.resource_name, campaign_criterion.status FROM campaign_criterion WHERE campaign_criterion.resource_name IN ({names})",
            "AdGroup" => $"SELECT ad_group.resource_name, ad_group.name, ad_group.status FROM ad_group WHERE ad_group.resource_name IN ({names})",
            "Keyword" => $"SELECT ad_group_criterion.resource_name, ad_group_criterion.status FROM ad_group_criterion WHERE ad_group_criterion.resource_name IN ({names})",
            "ResponsiveSearchAd" => $"SELECT ad_group_ad.resource_name, ad_group_ad.status FROM ad_group_ad WHERE ad_group_ad.resource_name IN ({names})",
            _ => null
        };
    }

    private static string? QueryKindFor(GoogleAdsPublishedResourceDto resource) => resource.TipoRecurso switch
    {
        "Budget" => "Budget",
        "Campaign" => "Campaign",
        "CampaignCriterion" or "NegativeKeyword" => "CampaignCriterion",
        "AdGroup" => "AdGroup",
        "Keyword" => "Keyword",
        "ResponsiveSearchAd" => "ResponsiveSearchAd",
        _ => null
    };

    private static string Escape(string value) => value.Replace("'", "\\'", StringComparison.Ordinal);

    private static HashSet<string> FoundResourceNames(string body)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return found;
        }

        foreach (var row in results.EnumerateArray())
        {
            foreach (var entity in new[] { "campaignBudget", "campaign", "campaignCriterion", "adGroup", "adGroupCriterion", "adGroupAd" })
            {
                if (row.TryGetProperty(entity, out var value)
                    && value.TryGetProperty("resourceName", out var resourceName)
                    && resourceName.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(resourceName.GetString()))
                {
                    found.Add(resourceName.GetString()!);
                    break;
                }
            }
        }

        return found;
    }
}
