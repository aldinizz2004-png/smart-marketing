using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Subscriptions;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    StatusInfo Status,
    MembershipRole MyRole,
    DateTimeOffset CreatedAt,
    SubscriptionResponse? CurrentSubscription);
