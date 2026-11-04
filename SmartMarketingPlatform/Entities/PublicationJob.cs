namespace SmartMarketingPlatform.Entities;

// Scheduled publishing work. It points to the approving review, so nothing is
// published without a human approval (the application checks the review is "Approved").
public class PublicationJob
{
    public Guid Id { get; set; }
    public Guid ContentItemId { get; set; }
    public Guid RevisionId { get; set; }
    public Guid ApprovedReviewId { get; set; }                  // FK -> ContentReview
    public Guid SocialAccountId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "PublicationJob")
    public string IdempotencyKey { get; set; } = string.Empty;  // prevents double publishing on retries
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public Guid? SourceRecommendationId { get; set; }           // set when the time came from a recommendation
    public DateTimeOffset CreatedAt { get; set; }

    public ContentItem ContentItem { get; set; } = null!;
    public ContentRevision Revision { get; set; } = null!;
    public ContentReview ApprovedReview { get; set; } = null!;
    public SocialAccount SocialAccount { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public StrategyRecommendation? SourceRecommendation { get; set; }
    public Publication? Publication { get; set; }
}
