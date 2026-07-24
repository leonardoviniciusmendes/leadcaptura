using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsOperationBuilder(
    IGoogleAdsGeoTargetResolver geoTargetResolver,
    IGoogleAdsLanguageResolver languageResolver) : IGoogleAdsOperationBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsOperationPlan> BuildAsync(GoogleAdsPlanoPublicacao preview, string customerId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<GoogleAdsPreviewPayload>(preview.PayloadPreviewJson, JsonOptions)
            ?? throw new InvalidOperationException("Payload do preview invalido.");
        var group = payload.AdGroups.First();
        var budgetResource = $"customers/{customerId}/campaignBudgets/-1";
        var campaignResource = $"customers/{customerId}/campaigns/-2";
        var adGroupResource = $"customers/{customerId}/adGroups/-3";
        var geo = await geoTargetResolver.ResolveAsync(preview.Pais, cancellationToken);
        var language = await languageResolver.ResolveAsync(preview.Idioma, cancellationToken);
        var warnings = new List<string>();
        if (!string.Equals(payload.Campaign.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status da campanha foi forçado para PAUSED.");
        if (!string.Equals(group.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status do grupo foi forçado para PAUSED.");
        if (!string.Equals(group.ResponsiveSearchAd.Status, "PAUSED", StringComparison.OrdinalIgnoreCase)) warnings.Add("Status do anúncio foi forçado para PAUSED.");

        var operations = new List<GoogleAdsOperationItem>
        {
            Op("Budget", payload.Budget.Name, "CampaignBudgetOperation", new { create = new { resourceName = budgetResource, name = payload.Budget.Name, amountMicros = payload.Budget.AmountMicros, deliveryMethod = "STANDARD", explicitlyShared = false } }),
            Op("Campaign", payload.Campaign.Name, "CampaignOperation", new { create = new { resourceName = campaignResource, name = payload.Campaign.Name, status = "PAUSED", advertisingChannelType = "SEARCH", campaignBudget = budgetResource, networkSettings = new { targetGoogleSearch = true, targetSearchNetwork = false, targetContentNetwork = false, targetPartnerSearchNetwork = false }, manualCpc = new { } } }),
            Op("CampaignCriterion", "Localizacao", "CampaignCriterionOperation", new { create = new { campaign = campaignResource, location = new { geoTargetConstant = geo } } }),
            Op("CampaignCriterion", "Idioma", "CampaignCriterionOperation", new { create = new { campaign = campaignResource, language = new { languageConstant = language } } })
        };

        operations.AddRange(group.NegativeKeywords.Select(x => Op("NegativeKeyword", x.Text, "CampaignCriterionOperation", new { create = new { campaign = campaignResource, negative = true, keyword = new { text = x.Text, matchType = x.MatchType } } })));
        operations.Add(Op("AdGroup", group.Name, "AdGroupOperation", new { create = new { resourceName = adGroupResource, campaign = campaignResource, name = group.Name, status = "PAUSED", cpcBidMicros = group.CpcBidMicros } }));
        operations.AddRange(group.Keywords.Select(x => Op("Keyword", x.Text, "AdGroupCriterionOperation", new { create = new { adGroup = adGroupResource, status = "PAUSED", keyword = new { text = x.Text, matchType = x.MatchType } } })));
        operations.Add(Op("ResponsiveSearchAd", payload.Campaign.Name, "AdGroupAdOperation", new
        {
            create = new
            {
                adGroup = adGroupResource,
                status = "PAUSED",
                ad = new
                {
                    finalUrls = group.ResponsiveSearchAd.FinalUrls,
                    responsiveSearchAd = new
                    {
                        headlines = group.ResponsiveSearchAd.Headlines.Select(x => new { text = x }).ToArray(),
                        descriptions = group.ResponsiveSearchAd.Descriptions.Select(x => new { text = x }).ToArray(),
                        path1 = group.ResponsiveSearchAd.Path1,
                        path2 = group.ResponsiveSearchAd.Path2
                    }
                }
            }
        }));

        return new GoogleAdsOperationPlan(preview.ConteudoHash, preview.Versao, customerId, geo, language, operations, warnings);
    }

    private static GoogleAdsOperationItem Op(string type, string name, string operation, object payload)
    {
        return new GoogleAdsOperationItem(type, name, operation, JsonSerializer.Serialize(payload, JsonOptions));
    }
}
