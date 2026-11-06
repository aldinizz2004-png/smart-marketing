using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Subscriptions;

public sealed record SubscriptionResponse(
    Guid Id,
    SubscriptionPlan Plan,
    int MaxPostsPerDay,
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

// GET /api/plans: what each plan allows and costs (for the pricing page).
public sealed record PlanResponse(
    SubscriptionPlan Plan,
    int MaxPostsPerDay,
    string Currency,
    IReadOnlyDictionary<SubscriptionDuration, decimal> Prices);
