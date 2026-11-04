using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Automation;

// IsConfigured = false means the owner never saved a policy and these are the safe defaults
// (automation off).
public sealed record AutomationPolicyResponse(
    Guid BusinessId,
    bool IsConfigured,
    bool AutomationEnabled,
    short MaxPostsPerDay,
    string? PublishWindowStart,
    string? PublishWindowEnd,
    decimal? MaxDiscountPercent,
    decimal? MonthlyAdBudget,
    string? BudgetCurrency,
    bool RequireSeparateReviewer,
    IReadOnlyList<string> AllowedPlatforms,
    IReadOnlyList<RecommendationAction> AutoApplyActions,
    Guid? UpdatedBy,
    DateTimeOffset? UpdatedAt);
