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
    public int? Width { get; set; }                             // images only, read from the file header
    public int? Height { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Filled when the AI looked at the image (once): reused as text in every later prompt.
    public string? VisualDescription { get; set; }
    public bool? ShowsProduct { get; set; }
    public string? QualityIssuesJson { get; set; }              // ["Dark","ClutteredBackground"]
    public string? QualityAdvice { get; set; }
    public DateTimeOffset? DescribedAt { get; set; }
    public Guid? DescribedByExecutionId { get; set; }

    public Business Business { get; set; } = null!;
    public Product? Product { get; set; }
    public AiExecution? DescribedByExecution { get; set; }
}
