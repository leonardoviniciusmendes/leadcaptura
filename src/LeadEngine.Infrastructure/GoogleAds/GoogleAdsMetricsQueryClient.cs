using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsMetricsQueryClient(GoogleAdsGaqlClient gaqlClient) : IGoogleAdsMetricsQueryClient
{
    public async Task<(string? RequestId, IReadOnlyList<GoogleAdsMetricsRow> Rows)> QueryMetricsAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, DateOnly dataInicial, DateOnly dataFinal, CancellationToken cancellationToken)
    {
        var query = GoogleAdsGaqlQueries.Metrics(campaignResourceName, dataInicial, dataFinal);
        var (requestId, body) = await gaqlClient.SearchAsync(customerId, accessToken, developerToken, query, cancellationToken);
        using (body)
        {
            return (requestId, Parse(body.RootElement));
        }
    }

    private static IReadOnlyList<GoogleAdsMetricsRow> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) return [];
        var rows = new List<GoogleAdsMetricsRow>();
        foreach (var item in results.EnumerateArray())
        {
            var campaign = item.GetProperty("campaign");
            var metrics = item.GetProperty("metrics");
            var segments = item.GetProperty("segments");
            var resourceName = S(campaign, "resourceName");
            rows.Add(new GoogleAdsMetricsRow(
                resourceName,
                S(campaign, "id"),
                DateOnly.Parse(S(segments, "date")),
                L(metrics, "impressions"),
                L(metrics, "clicks"),
                L(metrics, "costMicros"),
                D(metrics, "ctr"),
                L(metrics, "averageCpc"),
                D(metrics, "conversions"),
                D(metrics, "conversionsValue"),
                NullableD(metrics, "searchImpressionShare"),
                NullableD(metrics, "topImpressionPercentage"),
                NullableD(metrics, "absoluteTopImpressionPercentage")));
        }
        return rows;
    }

    private static string S(JsonElement e, string p) => e.TryGetProperty(p, out var v) ? v.ToString() : string.Empty;
    private static long L(JsonElement e, string p) => e.TryGetProperty(p, out var v) && long.TryParse(v.ToString(), out var l) ? l : 0;
    private static decimal D(JsonElement e, string p) => e.TryGetProperty(p, out var v) && decimal.TryParse(v.ToString(), out var d) ? d : 0;
    private static decimal? NullableD(JsonElement e, string p) => e.TryGetProperty(p, out var v) && decimal.TryParse(v.ToString(), out var d) ? d : null;
}
