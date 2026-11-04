using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A word the AI should prefer or never use for this brand.
public class BrandTerm
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }                        // FK -> BrandProfile
    public string Term { get; set; } = string.Empty;
    public BrandTermRule Rule { get; set; }
    public string? Note { get; set; }

    public BrandProfile BrandProfile { get; set; } = null!;
}
