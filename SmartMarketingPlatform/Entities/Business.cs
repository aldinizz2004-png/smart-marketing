namespace SmartMarketingPlatform.Entities;

// A shop or small company that the platform markets.
public class Business
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;        // e.g. "Cafe", "Bakery"
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;      // e.g. "West Bank Standard Time"
    public string DefaultCurrency { get; set; } = "ILS";
    public string? ContactPhone { get; set; }
    public string? Description { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "Business")
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public BrandProfile? BrandProfile { get; set; }              // one-to-one

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
    public ICollection<KnowledgeSource> KnowledgeSources { get; set; } = new List<KnowledgeSource>();
    public ICollection<AudienceSegment> AudienceSegments { get; set; } = new List<AudienceSegment>();
}