using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Subscriptions;

// The client only chooses the duration ("OneMonth", "ThreeMonths", "OneYear" or 1/3/12).
// Price, currency and dates are decided by the server.
public sealed class CreateSubscriptionRequest
{
    [Required, EnumDataType(typeof(SubscriptionDuration))]
    public SubscriptionDuration? Duration { get; init; }
}
