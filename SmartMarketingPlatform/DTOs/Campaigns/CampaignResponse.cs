using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Campaigns;

public sealed record CampaignResponse(
    Guid Id,
    Guid BusinessId,
    string Name,
    CampaignObjective Objective,
    string GoalStatement,
    string? TargetMetric,
    decimal? TargetValue,
    decimal? Budget,
    string? BudgetCurrency,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    StatusInfo Status,
    Guid CreatedBy,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> OfferIds,
    IReadOnlyList<Guid> AudienceSegmentIds,
    int? LatestPlanVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
