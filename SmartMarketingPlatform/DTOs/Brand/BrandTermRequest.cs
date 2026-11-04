using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Brand;

public sealed class BrandTermRequest
{
    [Required, MaxLength(100)]
    public string? Term { get; init; }

    [Required, EnumDataType(typeof(BrandTermRule))]
    public BrandTermRule? Rule { get; init; }                   // "Preferred" or "Prohibited"

    [MaxLength(500)]
    public string? Note { get; init; }
}
