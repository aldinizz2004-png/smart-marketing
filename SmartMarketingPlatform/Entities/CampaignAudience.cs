namespace SmartMarketingPlatform.Entities;

// Join entity: Campaign <-> AudienceSegment
public class CampaignAudience
{
    public Guid CampaignId { get; set; }
    public Guid AudienceSegmentId { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public AudienceSegment AudienceSegment { get; set; } = null!;
}
