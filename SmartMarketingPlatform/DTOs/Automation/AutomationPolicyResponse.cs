using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Automation;

// IsConfigured = false means the owner never saved a policy and these are the safe defaults
// (automation off).
public sealed record AutomationPolicyResponse(
    Guid BusinessId,
    bool IsConfigured,
    bool AutomationEnabled,
    short MaxPostsPerDay,
    SubscriptionPlan Plan,
    int PlanMaxPostsPerDay,          // the most the current plan allows; MaxPostsPerDay can never be higher
    string? PublishWindowStart,
    string? PublishWindowEnd,
    decimal? MaxDiscountPercent,
    decimal? MonthlyAdBudget,
    string? BudgetCurrency,
    bool RequireSeparateReviewer,
    bool AutoReplyToMessages,
    IReadOnlyList<string> AllowedPlatforms,
    IReadOnlyList<RecommendationAction> AutoApplyActions,
    Guid? UpdatedBy,
    DateTimeOffset? UpdatedAt);
