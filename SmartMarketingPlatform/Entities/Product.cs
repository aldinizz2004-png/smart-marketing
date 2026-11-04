namespace SmartMarketingPlatform.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ILS";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
}
