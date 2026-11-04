using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Options;

// Prices live in configuration, not in code or in the client request:
// the client chooses a duration, the server decides the price.
public sealed class SubscriptionPricingOptions
{
    public const string SectionName = "SubscriptionPricing";

    public string Currency { get; init; } = "USD";
    public Dictionary<SubscriptionDuration, decimal> Prices { get; init; } = new();
}
