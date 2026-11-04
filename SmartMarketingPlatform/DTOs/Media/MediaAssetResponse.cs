using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Media;

// Metadata only; the bytes are downloaded from ContentUrl.
public sealed record MediaAssetResponse(
    Guid Id,
    Guid BusinessId,
    Guid? ProductId,
    MediaKind Kind,
    string MimeType,
    long ByteSize,
    string? Sha256,
    string ContentUrl,
    DateTimeOffset CreatedAt);
