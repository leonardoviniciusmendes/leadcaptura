using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsResourceQueryClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver) : IGoogleAdsResourceQueryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>> CheckResourcesAsync(
        string customerId,
        string accessToken,
        string developerToken,
        IReadOnlyList<GoogleAdsPublishedResourceDto> resources,
        CancellationToken cancellationToken)
    {
        var result = new List<GoogleAdsPublishedResourceCheckDto>();
        foreach (var resource in resources.Where(x => !string.IsNullOrWhiteSpace(x.ResourceName)))
        {
            var check = await CheckOneAsync(customerId, accessToken, developerToken, resource, cancellationToken);
            result.Add(check);
        }

        return result;
    }

    private async Task<GoogleAdsPublishedResourceCheckDto> CheckOneAsync(string customerId, string accessToken, string developerToken, GoogleAdsPublishedResourceDto resource, CancellationToken cancellationToken)
    {
        var query = QueryFor(resource);
        if (query is null)
        {
            return new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, false, false, "Tipo de recurso sem consulta de reconciliacao.");
        }

        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiBaseUrl", cancellationToken)).Value ?? "https://googleads.googleapis.com/v22";
        var timeoutSeconds = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "ApiTimeoutSeconds", cancellationToken)).Value, out var timeout) ? timeout : 60;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 300)));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/customers/{GoogleAdsCustomerId.Normalize(customerId)}/googleAds:search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("developer-token", developerToken);
        request.Content = new StringContent(JsonSerializer.Serialize(new { query }, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("googleads").SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            return new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, false, false, "Nao foi possivel consultar o recurso no Google Ads.");
        }

        var found = HasResult(body);
        return new GoogleAdsPublishedResourceCheckDto(resource.TipoRecurso, resource.ResourceName, resource.ExternalId, resource.Nome, resource.Status, found, false, found ? "Recurso encontrado." : "Recurso ausente.");
    }

    private static string? QueryFor(GoogleAdsPublishedResourceDto resource)
    {
        var escaped = resource.ResourceName.Replace("'", "\\'", StringComparison.Ordinal);
        return resource.TipoRecurso switch
        {
            "Budget" => $"SELECT campaign_budget.resource_name, campaign_budget.name, campaign_budget.status FROM campaign_budget WHERE campaign_budget.resource_name = '{escaped}' LIMIT 1",
            "Campaign" => $"SELECT campaign.resource_name, campaign.name, campaign.status FROM campaign WHERE campaign.resource_name = '{escaped}' LIMIT 1",
            "CampaignCriterion" or "NegativeKeyword" => $"SELECT campaign_criterion.resource_name, campaign_criterion.status FROM campaign_criterion WHERE campaign_criterion.resource_name = '{escaped}' LIMIT 1",
            "AdGroup" => $"SELECT ad_group.resource_name, ad_group.name, ad_group.status FROM ad_group WHERE ad_group.resource_name = '{escaped}' LIMIT 1",
            "Keyword" => $"SELECT ad_group_criterion.resource_name, ad_group_criterion.status FROM ad_group_criterion WHERE ad_group_criterion.resource_name = '{escaped}' LIMIT 1",
            "ResponsiveSearchAd" => $"SELECT ad_group_ad.resource_name, ad_group_ad.status FROM ad_group_ad WHERE ad_group_ad.resource_name = '{escaped}' LIMIT 1",
            _ => null
        };
    }

    private static bool HasResult(string body)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0;
    }
}
