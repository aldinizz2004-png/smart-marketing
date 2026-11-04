using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Offers;

// Used by POST (create) and PUT (full replace).
// An offer has EITHER a fixed price OR a discount percent, not both.
public sealed class OfferRequest
{
    [Required, MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Range(0d, 999_999_999_999d)]
    public decimal? OfferPrice { get; init; }

    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be 3 upper-case letters (ISO 4217).")]
    public string? Currency { get; init; }                      // with OfferPrice; empty = business currency

    [Range(0.01d, 100d)]
    public decimal? DiscountPercent { get; init; }

    [Required]
    public DateTimeOffset? StartsAt { get; init; }

    [Required]
    public DateTimeOffset? EndsAt { get; init; }
}
