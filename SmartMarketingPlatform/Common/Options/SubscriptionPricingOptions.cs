using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Options;

// Prices and limits live in configuration, not in code or in the client request:
// the client chooses a plan and a duration, the server decides the price and what the plan allows.
public sealed class SubscriptionPricingOptions
{
    public const string SectionName = "SubscriptionPricing";

    public string Currency { get; init; } = "USD";
    public Dictionary<SubscriptionPlan, PlanOptions> Plans { get; init; } = new();
}

public sealed class PlanOptions
{
    // Posts each business of the tenant may publish per local day. The owner can set less, never more.
    public int MaxPostsPerDay { get; init; } = 3;
    public Dictionary<SubscriptionDuration, decimal> Prices { get; init; } = new();
}
