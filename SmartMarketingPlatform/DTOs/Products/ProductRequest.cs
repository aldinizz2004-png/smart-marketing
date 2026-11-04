using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Products;

// Used by POST (create) and PUT (full replace).
public sealed class ProductRequest
{
    [MaxLength(64)]
    public string? Sku { get; init; }

    [Required, MaxLength(200)]
    public string? Name { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required, Range(0d, 999_999_999_999d)]
    public decimal? Price { get; init; }

    // Empty = the business's default currency.
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be 3 upper-case letters (ISO 4217).")]
    public string? Currency { get; init; }

    public bool? IsActive { get; init; }                        // empty = true
}
