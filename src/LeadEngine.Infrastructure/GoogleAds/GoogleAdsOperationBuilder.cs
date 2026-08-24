using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsOperationBuilder(
    IGoogleAdsGeoTargetResolver geoTargetResolver,
    IGoogleAdsLanguageResolver languageResolver) : IGoogleAdsOperationBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsOperationPlan> BuildAsync(GoogleAdsPlanoPublicacao preview, string customerId, CancellationToken cancellationToken)
    {
        var normalizedCustomerId = GoogleAdsCustomerId.Normalize(customerId);
        var payload = JsonSerializer.Deserialize<GoogleAdsPreviewPayload>(preview.PayloadPreviewJson, JsonOptions)
            ?? throw new InvalidOperationException("Payload do preview invalido.");
        var group = payload.AdGroups.First();
        var budgetResource = $"customers/{normalizedCustomerId}/campaignBudgets/-1";
        var campaignResource = $"customers/{normalizedCustomerId}/campaigns/-2";
        var adGroupResource = $"customers/{normalizedCustomerId}/adGroups/-3";
        var geo = !string.IsNullOrWhiteSpace(payload.Campaign.GeoTargetResourceName)
            ? payload.Campaign.GeoTargetResourceName
            : await geoTargetResolver.ResolveAsync(preview.Pais, cancellationToken);
        var language = await languageResolver.ResolveAsync(preview.Idioma, cancellationToken);
        var warnings = new List<string>();
        if (!string.Equals(payload.Campaign.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status solicitado foi substituido por PAUSED por seguranca.");
        if (!string.Equals(group.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status solicitado foi substituido por PAUSED por seguranca.");
        if (!string.Equals(group.ResponsiveSearchAd.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status solicitado foi substituido por PAUSED por seguranca.");

        var operations = new List<GoogleAdsOperationItem>
        {
            Op("Budget", payload.Budget.Name, "CampaignBudgetOperation", budgetResource, new { resourceName = budgetResource, name = payload.Budget.Name, amountMicros = payload.Budget.AmountMicros }),
            Op("Campaign", payload.Campaign.Name, "CampaignOperation", campaignResource, new { resourceName = campaignResource, name = payload.Campaign.Name, budgetResource }),
            Op("CampaignCriterion", "Localizacao", "CampaignCriterionOperation", null, new { campaignResource, geoTargetResourceName = geo }),
            Op("CampaignCriterion", "Idioma", "CampaignCriterionOperation", null, new { campaignResource, languageResourceName = language })
        };

        operations.AddRange(group.NegativeKeywords.Select(x => Op("NegativeKeyword", x.Text, "CampaignCriterionOperation", null, new { campaignResource, negative = true, text = x.Text, matchType = x.MatchType })));
        operations.Add(Op("AdGroup", group.Name, "AdGroupOperation", adGroupResource, new { resourceName = adGroupResource, campaignResource, name = group.Name, cpcBidMicros = group.CpcBidMicros }));
        operations.AddRange(group.Keywords.Select(x => Op("Keyword", x.Text, "AdGroupCriterionOperation", null, new { adGroupResource, text = x.Text, matchType = x.MatchType })));
        operations.Add(Op("ResponsiveSearchAd", payload.Campaign.Name, "AdGroupAdOperation", null, new
        {
            adGroupResource,
            finalUrls = group.ResponsiveSearchAd.FinalUrls,
            headlines = group.ResponsiveSearchAd.Headlines,
            descriptions = group.ResponsiveSearchAd.Descriptions,
            path1 = group.ResponsiveSearchAd.Path1,
            path2 = group.ResponsiveSearchAd.Path2
        }));

        return new GoogleAdsOperationPlan(preview.ConteudoHash, preview.Versao, normalizedCustomerId, geo, language, operations, warnings);
    }

    private static GoogleAdsOperationItem Op(string type, string name, string operation, string? temporaryResourceName, object payload)
    {
        return new GoogleAdsOperationItem(type, name, operation, JsonSerializer.Serialize(payload, JsonOptions), temporaryResourceName);
    }
}
