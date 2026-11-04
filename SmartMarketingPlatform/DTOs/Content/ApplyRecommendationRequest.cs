namespace SmartMarketingPlatform.DTOs.Content;

// Needed only when the recommendation came from a business-wide run (no campaign of its own).
public sealed class ApplyRecommendationRequest
{
    public Guid? CampaignId { get; init; }
}
