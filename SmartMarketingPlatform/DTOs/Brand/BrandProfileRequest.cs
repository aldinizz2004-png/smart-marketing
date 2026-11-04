using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Brand;

// PUT (create or replace) the tone of voice the AI uses for this business.
public sealed class BrandProfileRequest
{
    [Required, MaxLength(50)]
    public string? Dialect { get; init; }                       // e.g. "Palestinian Arabic"

    [Required, MaxLength(50)]
    public string? Tone { get; init; }                          // e.g. "Friendly"

    [Required, Range(0, 3)]
    public byte? EmojiLevel { get; init; }                      // 0 = none ... 3 = a lot

    [MaxLength(4000)]
    public string? Guidelines { get; init; }

    [MaxLength(10)]
    public List<string>? ColorPalette { get; init; }            // ["#1F3A5F", "#F2A541"]
}
