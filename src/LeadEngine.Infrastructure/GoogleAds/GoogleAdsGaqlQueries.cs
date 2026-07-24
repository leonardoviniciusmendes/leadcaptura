namespace LeadEngine.Infrastructure.GoogleAds;

public static class GoogleAdsGaqlQueries
{
    public static string Metrics(string campaignResourceName, DateOnly start, DateOnly end)
    {
        var resource = campaignResourceName.Replace("'", "\\'", StringComparison.Ordinal);
        return $"""
            SELECT
              campaign.id,
              campaign.name,
              campaign.status,
              campaign.resource_name,
              segments.date,
              metrics.impressions,
              metrics.clicks,
              metrics.ctr,
              metrics.average_cpc,
              metrics.cost_micros,
              metrics.conversions,
              metrics.conversions_value,
              metrics.search_impression_share,
              metrics.top_impression_percentage,
              metrics.absolute_top_impression_percentage
            FROM campaign
            WHERE campaign.resource_name = '{resource}'
              AND segments.date BETWEEN '{start:yyyy-MM-dd}' AND '{end:yyyy-MM-dd}'
            ORDER BY segments.date
            """;
    }

    public static string CampaignStatus(string campaignResourceName)
    {
        var resource = campaignResourceName.Replace("'", "\\'", StringComparison.Ordinal);
        return $"SELECT campaign.id, campaign.name, campaign.status, campaign.resource_name FROM campaign WHERE campaign.resource_name = '{resource}' LIMIT 1";
    }
}
