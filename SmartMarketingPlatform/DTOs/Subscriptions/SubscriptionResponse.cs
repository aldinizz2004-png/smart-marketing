using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Subscriptions;

public sealed record SubscriptionResponse(
    Guid Id,
    SubscriptionDuration Duration,
    decimal Price,
    string Currency,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    StatusInfo Status,
    Guid? RenewedFromSubscriptionId,
    DateTimeOffset? CanceledAt,
    bool GrantsAccessNow,
    DateTimeOffset CreatedAt);
