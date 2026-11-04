using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A change the AI proposes: new content, another format, another time ...
public class StrategyRecommendation
{
    public Guid Id { get; set; }
    public Guid StrategyRunId { get; set; }
    public Guid? CampaignId { get; set; }
    public RecommendationAction ActionType { get; set; }
    public string ProposedPayloadJson { get; set; } = "{}";
    public DateTimeOffset? ProposedPublishAt { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }                    // 0 .. 1
    public int StatusId { get; set; }                           // Statuses (Category = "Recommendation")
    public bool AppliedAutomatically { get; set; }              // true = allowed by an AutoApplyRule
    public Guid? DecidedBy { get; set; }                        // FK -> AppUser (null when automatic)
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public StrategyRun StrategyRun { get; set; } = null!;
    public Campaign? Campaign { get; set; }
    public Status Status { get; set; } = null!;
    public AppUser? DecidedByUser { get; set; }
    public ICollection<RecommendationEvidence> Evidence { get; set; } = new List<RecommendationEvidence>();
}
