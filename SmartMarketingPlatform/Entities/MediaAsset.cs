using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Metadata of a file kept in object storage (the file itself is not stored in the database).
public class MediaAsset
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ProductId { get; set; }                        // optional: photo of a product
    public MediaKind Kind { get; set; }
    public string ObjectKey { get; set; } = string.Empty;       // path in object storage
    public string MimeType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public string? Sha256 { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Product? Product { get; set; }
}
