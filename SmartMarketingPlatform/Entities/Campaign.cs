using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// The owner's goal (e.g. "more orders this month") and its target.
public class Campaign
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CreatedBy { get; set; }                         // FK -> AppUser
    public string Name { get; set; } = string.Empty;
    public CampaignObjective Objective { get; set; }
    public string GoalStatement { get; set; } = string.Empty;   // the goal in the owner's words
    public string? TargetMetric { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? Budget { get; set; }
    public string? BudgetCurrency { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "Campaign")
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
    public Status Status { get; set; } = null!;

    public ICollection<CampaignPlan> Plans { get; set; } = new List<CampaignPlan>();
    public ICollection<ContentItem> ContentItems { get; set; } = new List<ContentItem>();

    // Many-to-many through explicit join entities
    public ICollection<CampaignProduct> CampaignProducts { get; set; } = new List<CampaignProduct>();
    public ICollection<CampaignAudience> CampaignAudiences { get; set; } = new List<CampaignAudience>();
    public ICollection<CampaignOffer> CampaignOffers { get; set; } = new List<CampaignOffer>();
}
