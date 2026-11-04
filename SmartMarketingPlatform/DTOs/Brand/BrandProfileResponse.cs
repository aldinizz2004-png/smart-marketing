namespace SmartMarketingPlatform.DTOs.Brand;

public sealed record BrandProfileResponse(
    Guid BusinessId,
    string Dialect,
    string Tone,
    byte EmojiLevel,
    string? Guidelines,
    IReadOnlyList<string> ColorPalette,
    IReadOnlyList<BrandTermResponse> Terms,
    DateTimeOffset UpdatedAt);
