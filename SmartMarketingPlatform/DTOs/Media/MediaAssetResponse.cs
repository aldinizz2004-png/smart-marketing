using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Media;

// Metadata only; the bytes are downloaded from ContentUrl.
// The AI fields are empty until the image is described (POST .../media/{id}/describe, or when it is used in a post).
public sealed record MediaAssetResponse(
    Guid Id,
    Guid BusinessId,
    Guid? ProductId,
    MediaKind Kind,
    string MimeType,
    long ByteSize,
    string? Sha256,
    string ContentUrl,
    int? Width,
    int? Height,
    string? VisualDescription,
    bool? ShowsProduct,
    IReadOnlyList<ImageQualityIssue> QualityIssues,
    string? QualityAdvice,
    DateTimeOffset? DescribedAt,
    DateTimeOffset CreatedAt);
