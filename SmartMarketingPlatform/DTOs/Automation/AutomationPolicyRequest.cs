using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Automation;

// The owner's limits on what the platform may do alone (PUT = full replace).
public sealed class AutomationPolicyRequest
{
    [Required]
    public bool? AutomationEnabled { get; init; }               // master switch

    [Required, Range(1, 50)]
    public short? MaxPostsPerDay { get; init; }

    // Business local time "HH:mm"; send both or neither. Start must be before End.
    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Use HH:mm, e.g. 09:00.")]
    public string? PublishWindowStart { get; init; }

    [RegularExpression("^([01][0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Use HH:mm, e.g. 21:30.")]
    public string? PublishWindowEnd { get; init; }

    [Range(0d, 100d)]
    public decimal? MaxDiscountPercent { get; init; }

    [Range(0d, 999_999_999_999d)]
    public decimal? MonthlyAdBudget { get; init; }

    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "BudgetCurrency must be 3 upper-case letters (ISO 4217).")]
    public string? BudgetCurrency { get; init; }                // empty = business currency

    public bool RequireSeparateReviewer { get; init; }          // the author cannot approve their own content

    [MaxLength(20)]
    public List<string> AllowedPlatforms { get; init; } = [];   // platform codes where auto-publishing is allowed

    [MaxLength(10)]
    public List<RecommendationAction> AutoApplyActions { get; init; } = [];
}
