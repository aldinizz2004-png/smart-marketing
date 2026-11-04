namespace SmartMarketingPlatform.Entities;

// The shop's tone of voice. One-to-one with Business:
// BusinessId is both the primary key and the foreign key.
public class BrandProfile
{
    public Guid BusinessId { get; set; }
    public string Dialect { get; set; } = string.Empty;         // e.g. "Palestinian Arabic"
    public string Tone { get; set; } = string.Empty;            // e.g. "Friendly"
    public byte EmojiLevel { get; set; } = 1;                   // 0 = none ... 3 = a lot
    public string? Guidelines { get; set; }
    public string? ColorPaletteJson { get; set; }               // e.g. ["#1F3A5F","#F2A541"]
    public DateTimeOffset UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<BrandTerm> Terms { get; set; } = new List<BrandTerm>();
}
