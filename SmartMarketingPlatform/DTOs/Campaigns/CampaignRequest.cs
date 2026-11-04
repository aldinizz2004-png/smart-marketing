using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Campaigns;

// The owner's goal. Used by POST (create) and PUT (full replace).
public sealed class CampaignRequest
{
    [Required, MaxLength(200)]
    public string? Name { get; init; }

    [Required, EnumDataType(typeof(CampaignObjective))]
    public CampaignObjective? Objective { get; init; }

    [Required, MaxLength(1000)]
    public string? GoalStatement { get; init; }                 // in the owner's words: "More evening orders this month"

    [MaxLength(50)]
    public string? TargetMetric { get; init; }                  // e.g. "orders"

    [Range(0d, 1e12)]
    public decimal? TargetValue { get; init; }                  // e.g. 300

    [Range(0d, 999_999_999_999d)]
    public decimal? Budget { get; init; }

    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "BudgetCurrency must be 3 upper-case letters (ISO 4217).")]
    public string? BudgetCurrency { get; init; }                // empty = business currency

    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }

    [MaxLength(50)] public List<Guid> ProductIds { get; init; } = [];
    [MaxLength(50)] public List<Guid> OfferIds { get; init; } = [];
    [MaxLength(50)] public List<Guid> AudienceSegmentIds { get; init; } = [];
}
