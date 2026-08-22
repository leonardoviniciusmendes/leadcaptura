namespace LeadEngine.Application.Services;

public static class MetaAdsConstants
{
    public const string StatusPaused = "PAUSED";
    public const string BuyingTypeAuction = "AUCTION";
    public const string ObjectiveOutcomeLeads = "OUTCOME_LEADS";
    public const string BillingEventImpressions = "IMPRESSIONS";
    public const string OptimizationGoalLeadGeneration = "LEAD_GENERATION";
    public const string BidStrategyLowestCostWithoutCap = "LOWEST_COST_WITHOUT_CAP";
    public const bool IsAdsetBudgetSharingEnabled = false;

    public static readonly IReadOnlyList<string> NoSpecialAdCategories = [];
}
