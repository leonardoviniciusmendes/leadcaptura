using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsDiagnosticsQueryClient(GoogleAdsGaqlClient gaqlClient) : IGoogleAdsDiagnosticsQueryClient
{
    public async Task<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>> GetCampaignsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
              campaign.resource_name,
              campaign.id,
              campaign.name,
              campaign.status
            FROM campaign
            ORDER BY campaign.id DESC
            LIMIT 100
            """;
        var (_, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, query, cancellationToken);
        using (body)
        {
            if (!body.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return results.EnumerateArray()
                .Select(ToCampaign)
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceName))
                .ToArray();
        }
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>> GetAdGroupsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
              ad_group.resource_name,
              ad_group.id,
              ad_group.name,
              ad_group.status,
              campaign.resource_name
            FROM ad_group
            ORDER BY ad_group.id DESC
            LIMIT 100
            """;
        var (_, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, query, cancellationToken);
        using (body)
        {
            if (!body.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return results.EnumerateArray()
                .Select(ToAdGroup)
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceName))
                .ToArray();
        }
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>> GetKeywordsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
              ad_group_criterion.resource_name,
              ad_group_criterion.criterion_id,
              ad_group_criterion.status,
              ad_group_criterion.keyword.text,
              ad_group_criterion.keyword.match_type,
              ad_group.resource_name
            FROM ad_group_criterion
            WHERE ad_group_criterion.type = KEYWORD
              AND ad_group_criterion.negative = false
            ORDER BY ad_group_criterion.criterion_id DESC
            LIMIT 100
            """;
        var (_, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, query, cancellationToken);
        using (body)
        {
            if (!body.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return results.EnumerateArray()
                .Select(ToKeyword)
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceName))
                .ToArray();
        }
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>> GetResponsiveSearchAdsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
              ad_group_ad.resource_name,
              ad_group_ad.ad.id,
              ad_group_ad.status,
              ad_group.resource_name
            FROM ad_group_ad
            WHERE ad_group_ad.ad.type = RESPONSIVE_SEARCH_AD
            ORDER BY ad_group_ad.ad.id DESC
            LIMIT 100
            """;
        var (_, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, query, cancellationToken);
        using (body)
        {
            if (!body.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return results.EnumerateArray()
                .Select(ToResponsiveSearchAd)
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceName))
                .ToArray();
        }
    }

    private static GoogleAdsDiagnosticCampaignDto ToCampaign(JsonElement row)
    {
        if (!row.TryGetProperty("campaign", out var campaign) || campaign.ValueKind != JsonValueKind.Object)
        {
            return new GoogleAdsDiagnosticCampaignDto(string.Empty, string.Empty, null, null);
        }

        return new GoogleAdsDiagnosticCampaignDto(
            S(campaign, "resourceName") ?? string.Empty,
            S(campaign, "id") ?? string.Empty,
            S(campaign, "name"),
            S(campaign, "status"));
    }

    private static GoogleAdsDiagnosticAdGroupDto ToAdGroup(JsonElement row)
    {
        if (!row.TryGetProperty("adGroup", out var adGroup) || adGroup.ValueKind != JsonValueKind.Object)
        {
            return new GoogleAdsDiagnosticAdGroupDto(string.Empty, string.Empty, null, null, null);
        }

        var campaignResourceName = row.TryGetProperty("campaign", out var campaign) && campaign.ValueKind == JsonValueKind.Object
            ? S(campaign, "resourceName")
            : null;

        return new GoogleAdsDiagnosticAdGroupDto(
            S(adGroup, "resourceName") ?? string.Empty,
            S(adGroup, "id") ?? string.Empty,
            S(adGroup, "name"),
            S(adGroup, "status"),
            campaignResourceName);
    }

    private static GoogleAdsDiagnosticKeywordDto ToKeyword(JsonElement row)
    {
        if (!row.TryGetProperty("adGroupCriterion", out var criterion) || criterion.ValueKind != JsonValueKind.Object)
        {
            return new GoogleAdsDiagnosticKeywordDto(string.Empty, string.Empty, null, null, null, null);
        }

        var keyword = criterion.TryGetProperty("keyword", out var keywordElement) && keywordElement.ValueKind == JsonValueKind.Object
            ? keywordElement
            : default;
        var adGroupResourceName = row.TryGetProperty("adGroup", out var adGroup) && adGroup.ValueKind == JsonValueKind.Object
            ? S(adGroup, "resourceName")
            : null;

        return new GoogleAdsDiagnosticKeywordDto(
            S(criterion, "resourceName") ?? string.Empty,
            S(criterion, "criterionId") ?? string.Empty,
            keyword.ValueKind == JsonValueKind.Object ? S(keyword, "text") : null,
            keyword.ValueKind == JsonValueKind.Object ? S(keyword, "matchType") : null,
            S(criterion, "status"),
            adGroupResourceName);
    }

    private static GoogleAdsDiagnosticResponsiveSearchAdDto ToResponsiveSearchAd(JsonElement row)
    {
        if (!row.TryGetProperty("adGroupAd", out var adGroupAd) || adGroupAd.ValueKind != JsonValueKind.Object)
        {
            return new GoogleAdsDiagnosticResponsiveSearchAdDto(string.Empty, string.Empty, null, null);
        }

        var ad = adGroupAd.TryGetProperty("ad", out var adElement) && adElement.ValueKind == JsonValueKind.Object
            ? adElement
            : default;
        var adGroupResourceName = row.TryGetProperty("adGroup", out var adGroup) && adGroup.ValueKind == JsonValueKind.Object
            ? S(adGroup, "resourceName")
            : null;

        return new GoogleAdsDiagnosticResponsiveSearchAdDto(
            S(adGroupAd, "resourceName") ?? string.Empty,
            ad.ValueKind == JsonValueKind.Object ? S(ad, "id") ?? string.Empty : string.Empty,
            S(adGroupAd, "status"),
            adGroupResourceName);
    }

    private static string? S(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? value.ToString() : null;
    }
}
