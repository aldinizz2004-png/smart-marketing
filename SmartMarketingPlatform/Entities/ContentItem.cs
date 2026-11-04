using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// One post / story / reel / message slot inside a campaign.
public class ContentItem
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string PlatformCode { get; set; } = string.Empty;    // FK -> Platform (where it will be published)
    public ContentType ContentType { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "ContentItem")
    public DateTimeOffset? PlannedPublishAt { get; set; }
    public Guid? VariantGroupId { get; set; }                   // A/B test: items in one test share this id
    public string? VariantLabel { get; set; }                   // "A", "B" ...
    public Guid? SourceRecommendationId { get; set; }           // created by an AI recommendation
    public Guid? InspiredBySourcePostId { get; set; }           // inspired by a competitor post
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public Platform Platform { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public StrategyRecommendation? SourceRecommendation { get; set; }
    public SourcePost? InspiredBySourcePost { get; set; }
    public ICollection<ContentRevision> Revisions { get; set; } = new List<ContentRevision>();
}
