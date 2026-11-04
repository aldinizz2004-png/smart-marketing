using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// One row per recommendation type the owner lets the AI apply on its own.
public class AutoApplyRule
{
    public Guid BusinessId { get; set; }                        // FK -> AutomationPolicy
    public RecommendationAction ActionType { get; set; }

    public AutomationPolicy Policy { get; set; } = null!;
}
