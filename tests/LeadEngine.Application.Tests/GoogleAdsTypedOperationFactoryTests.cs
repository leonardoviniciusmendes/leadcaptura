using LeadEngine.Application.DTOs;
using LeadEngine.Infrastructure.GoogleAds;
using Google.Ads.GoogleAds.V22.Enums;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsTypedOperationFactoryTests
{
    [Fact]
    public void CriaMutateOperationsTipadasComStatusPaused()
    {
        var plan = new GoogleAdsOperationPlan("HASH", 1, "1234567890", "geoTargetConstants/2076", "languageConstants/1014",
        [
            new("Budget", "Budget", "CampaignBudgetOperation", "{\"resourceName\":\"customers/1234567890/campaignBudgets/-1\",\"name\":\"Budget\",\"amountMicros\":10000000}", "customers/1234567890/campaignBudgets/-1"),
            new("Campaign", "Campanha", "CampaignOperation", "{\"resourceName\":\"customers/1234567890/campaigns/-2\",\"name\":\"Campanha\",\"budgetResource\":\"customers/1234567890/campaignBudgets/-1\"}", "customers/1234567890/campaigns/-2"),
            new("CampaignCriterion", "Localizacao", "CampaignCriterionOperation", "{\"campaignResource\":\"customers/1234567890/campaigns/-2\",\"geoTargetResourceName\":\"geoTargetConstants/2076\"}"),
            new("CampaignCriterion", "Idioma", "CampaignCriterionOperation", "{\"campaignResource\":\"customers/1234567890/campaigns/-2\",\"languageResourceName\":\"languageConstants/1014\"}"),
            new("AdGroup", "Grupo", "AdGroupOperation", "{\"resourceName\":\"customers/1234567890/adGroups/-3\",\"campaignResource\":\"customers/1234567890/campaigns/-2\",\"name\":\"Grupo\",\"cpcBidMicros\":1000000}", "customers/1234567890/adGroups/-3"),
            new("Keyword", "plano saude", "AdGroupCriterionOperation", "{\"adGroupResource\":\"customers/1234567890/adGroups/-3\",\"text\":\"plano saude\",\"matchType\":\"PHRASE\"}"),
            new("ResponsiveSearchAd", "RSA", "AdGroupAdOperation", "{\"adGroupResource\":\"customers/1234567890/adGroups/-3\",\"finalUrls\":[\"https://leadengine.test/lp/x\"],\"headlines\":[\"Titulo Um\",\"Titulo Dois\",\"Titulo Tres\"],\"descriptions\":[\"Descricao um\",\"Descricao dois\"],\"path1\":\"plano\",\"path2\":\"saude\"}")
        ], []);

        var operations = new GoogleAdsTypedOperationFactory().Create(plan);

        Assert.Equal(7, operations.Count);
        Assert.NotNull(operations[0].CampaignBudgetOperation);
        Assert.NotNull(operations[1].CampaignOperation);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.CampaignStatusEnum.Types.CampaignStatus.Paused, operations[1].CampaignOperation.Create.Status);
        Assert.Equal(
            EuPoliticalAdvertisingStatusEnum.Types.EuPoliticalAdvertisingStatus.DoesNotContainEuPoliticalAdvertising,
            operations[1].CampaignOperation.Create.ContainsEuPoliticalAdvertising);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupStatusEnum.Types.AdGroupStatus.Paused, operations[4].AdGroupOperation.Create.Status);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupAdStatusEnum.Types.AdGroupAdStatus.Paused, operations[6].AdGroupAdOperation.Create.Status);
    }
}
