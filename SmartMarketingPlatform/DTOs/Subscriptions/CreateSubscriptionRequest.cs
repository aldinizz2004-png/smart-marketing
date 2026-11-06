using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Subscriptions;

// The client chooses the plan ("Basic", "Pro", "Business"; empty = Basic) and the duration
// ("OneMonth", "ThreeMonths", "OneYear" or 1/3/12). Price, currency, limits and dates are decided by the server.
public sealed class CreateSubscriptionRequest
{
    [EnumDataType(typeof(SubscriptionPlan))]
    public SubscriptionPlan? Plan { get; init; }

    [Required, EnumDataType(typeof(SubscriptionDuration))]
    public SubscriptionDuration? Duration { get; init; }
}
