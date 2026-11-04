namespace SmartMarketingPlatform.Entities;

// Join entity: Campaign <-> Offer
public class CampaignOffer
{
    public Guid CampaignId { get; set; }
    public Guid OfferId { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public Offer Offer { get; set; } = null!;
}
