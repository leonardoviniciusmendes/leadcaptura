using System.Text.Json;
using Google.Protobuf;
using Google.Ads.GoogleAds.V22.Common;
using Google.Ads.GoogleAds.V22.Enums;
using Google.Ads.GoogleAds.V22.Resources;
using Google.Ads.GoogleAds.V22.Services;
using LeadEngine.Application.DTOs;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsTypedOperationFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IReadOnlyList<MutateOperation> Create(GoogleAdsOperationPlan plan)
    {
        return plan.Operations.Select(CreateOperation).ToArray();
    }

    public string ToGoogleAdsJson(GoogleAdsOperationPlan plan)
    {
        var request = new MutateGoogleAdsRequest
        {
            CustomerId = plan.CustomerId,
            PartialFailure = false,
            ValidateOnly = false
        };
        request.MutateOperations.AddRange(Create(plan));
        return JsonSerializer.Serialize(new
        {
            mutateOperations = request.MutateOperations.Select(x => JsonSerializer.Deserialize<object>(JsonFormatter.Default.Format(x), JsonOptions)).ToArray(),
            partialFailure = false,
            validateOnly = false
        }, JsonOptions);
    }

    private static MutateOperation CreateOperation(GoogleAdsOperationItem item)
    {
        using var doc = JsonDocument.Parse(item.PayloadJson);
        var root = doc.RootElement;
        return item.TipoRecurso switch
        {
            "Budget" => new MutateOperation { CampaignBudgetOperation = new CampaignBudgetOperation { Create = Budget(root) } },
            "Campaign" => new MutateOperation { CampaignOperation = new CampaignOperation { Create = Campaign(root) } },
            "CampaignCriterion" => new MutateOperation { CampaignCriterionOperation = new CampaignCriterionOperation { Create = CampaignCriterion(root) } },
            "NegativeKeyword" => new MutateOperation { CampaignCriterionOperation = new CampaignCriterionOperation { Create = NegativeKeyword(root) } },
            "AdGroup" => new MutateOperation { AdGroupOperation = new AdGroupOperation { Create = AdGroup(root) } },
            "Keyword" => new MutateOperation { AdGroupCriterionOperation = new AdGroupCriterionOperation { Create = Keyword(root) } },
            "ResponsiveSearchAd" => new MutateOperation { AdGroupAdOperation = new AdGroupAdOperation { Create = ResponsiveSearchAd(root) } },
            _ => throw new InvalidOperationException($"Tipo de operacao Google Ads nao suportado: {item.TipoRecurso}.")
        };
    }

    private static CampaignBudget Budget(JsonElement root)
    {
        return new CampaignBudget
        {
            ResourceName = S(root, "resourceName"),
            Name = S(root, "name"),
            AmountMicros = L(root, "amountMicros"),
            DeliveryMethod = BudgetDeliveryMethodEnum.Types.BudgetDeliveryMethod.Standard,
            ExplicitlyShared = false
        };
    }

    private static Campaign Campaign(JsonElement root)
    {
        return new Campaign
        {
            ResourceName = S(root, "resourceName"),
            Name = S(root, "name"),
            Status = CampaignStatusEnum.Types.CampaignStatus.Paused,
            AdvertisingChannelType = AdvertisingChannelTypeEnum.Types.AdvertisingChannelType.Search,
            CampaignBudget = S(root, "budgetResource"),
            NetworkSettings = new Campaign.Types.NetworkSettings
            {
                TargetGoogleSearch = true,
                TargetSearchNetwork = false,
                TargetContentNetwork = false,
                TargetPartnerSearchNetwork = false
            },
            ManualCpc = new ManualCpc()
        };
    }

    private static CampaignCriterion CampaignCriterion(JsonElement root)
    {
        if (root.TryGetProperty("geoTargetResourceName", out var geo))
        {
            return new CampaignCriterion
            {
                Campaign = S(root, "campaignResource"),
                Location = new LocationInfo { GeoTargetConstant = geo.GetString() ?? string.Empty }
            };
        }

        return new CampaignCriterion
        {
            Campaign = S(root, "campaignResource"),
            Language = new LanguageInfo { LanguageConstant = S(root, "languageResourceName") }
        };
    }

    private static CampaignCriterion NegativeKeyword(JsonElement root)
    {
        return new CampaignCriterion
        {
            Campaign = S(root, "campaignResource"),
            Negative = true,
            Keyword = new KeywordInfo
            {
                Text = S(root, "text"),
                MatchType = MatchType(S(root, "matchType"))
            }
        };
    }

    private static AdGroup AdGroup(JsonElement root)
    {
        var adGroup = new AdGroup
        {
            ResourceName = S(root, "resourceName"),
            Campaign = S(root, "campaignResource"),
            Name = S(root, "name"),
            Status = AdGroupStatusEnum.Types.AdGroupStatus.Paused
        };
        if (root.TryGetProperty("cpcBidMicros", out var cpc) && cpc.ValueKind == JsonValueKind.Number && cpc.TryGetInt64(out var micros) && micros > 0)
        {
            adGroup.CpcBidMicros = micros;
        }

        return adGroup;
    }

    private static AdGroupCriterion Keyword(JsonElement root)
    {
        return new AdGroupCriterion
        {
            AdGroup = S(root, "adGroupResource"),
            Status = AdGroupCriterionStatusEnum.Types.AdGroupCriterionStatus.Paused,
            Keyword = new KeywordInfo
            {
                Text = S(root, "text"),
                MatchType = MatchType(S(root, "matchType"))
            }
        };
    }

    private static AdGroupAd ResponsiveSearchAd(JsonElement root)
    {
        var ad = new Ad
        {
            ResponsiveSearchAd = new ResponsiveSearchAdInfo
            {
                Path1 = S(root, "path1"),
                Path2 = S(root, "path2")
            }
        };
        ad.FinalUrls.AddRange(StringArray(root, "finalUrls"));
        ad.ResponsiveSearchAd.Headlines.AddRange(StringArray(root, "headlines").Select(x => new AdTextAsset { Text = x }));
        ad.ResponsiveSearchAd.Descriptions.AddRange(StringArray(root, "descriptions").Select(x => new AdTextAsset { Text = x }));

        return new AdGroupAd
        {
            AdGroup = S(root, "adGroupResource"),
            Status = AdGroupAdStatusEnum.Types.AdGroupAdStatus.Paused,
            Ad = ad
        };
    }

    private static KeywordMatchTypeEnum.Types.KeywordMatchType MatchType(string value)
    {
        return value.Equals("Exact", StringComparison.OrdinalIgnoreCase) || value.Equals("EXACT", StringComparison.OrdinalIgnoreCase)
            ? KeywordMatchTypeEnum.Types.KeywordMatchType.Exact
            : value.Equals("Broad", StringComparison.OrdinalIgnoreCase) || value.Equals("BROAD", StringComparison.OrdinalIgnoreCase)
                ? KeywordMatchTypeEnum.Types.KeywordMatchType.Broad
                : KeywordMatchTypeEnum.Types.KeywordMatchType.Phrase;
    }

    private static string S(JsonElement root, string property) => root.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;
    private static long L(JsonElement root, string property) => root.TryGetProperty(property, out var value) && value.TryGetInt64(out var number) ? number : 0;
    private static IReadOnlyList<string> StringArray(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray()
            : [];
    }
}
