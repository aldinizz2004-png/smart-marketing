namespace SmartMarketingPlatform.Entities;

public class Offer
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? OfferPrice { get; set; }
    public string? Currency { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "Offer")
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Status Status { get; set; } = null!;
}
