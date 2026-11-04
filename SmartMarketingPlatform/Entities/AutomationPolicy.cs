namespace SmartMarketingPlatform.Entities;

// The limits the business OWNER sets for the platform. One-to-one with Business.
public class AutomationPolicy
{
    public Guid BusinessId { get; set; }                        // primary key + FK -> Business
    public bool AutomationEnabled { get; set; } = true;         // owner kill switch
    public short MaxPostsPerDay { get; set; } = 3;
    public TimeOnly? PublishWindowStart { get; set; }           // business local time
    public TimeOnly? PublishWindowEnd { get; set; }
    public decimal? MaxDiscountPercent { get; set; }
    public decimal? MonthlyAdBudget { get; set; }
    public string? BudgetCurrency { get; set; }
    public bool RequireSeparateReviewer { get; set; }           // author cannot approve own content
    public Guid UpdatedBy { get; set; }                         // FK -> AppUser
    public DateTimeOffset UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public AppUser UpdatedByUser { get; set; } = null!;

    // Many-to-many with Platform: where the platform is allowed to publish.
    public ICollection<AutomationPolicyPlatform> AllowedPlatforms { get; set; } = new List<AutomationPolicyPlatform>();

    // Recommendation types the AI may apply without asking the owner.
    public ICollection<AutoApplyRule> AutoApplyRules { get; set; } = new List<AutoApplyRule>();
}
