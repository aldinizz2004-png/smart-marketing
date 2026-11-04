using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Businesses;

// Used by POST (create) and PUT (full replace): every required field must be sent.
public sealed class BusinessRequest
{
    [Required, MaxLength(200)]
    public string? Name { get; init; }

    [Required, MaxLength(100)]
    public string? Category { get; init; }                      // e.g. "Cafe", "Bakery"

    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "CountryCode must be 2 upper-case letters (ISO 3166), e.g. PS.")]
    public string? CountryCode { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [Required, MaxLength(64)]
    public string? TimeZoneId { get; init; }                    // e.g. "Asia/Hebron"

    [Required, RegularExpression("^[A-Z]{3}$", ErrorMessage = "DefaultCurrency must be 3 upper-case letters (ISO 4217), e.g. ILS.")]
    public string? DefaultCurrency { get; init; }

    [MaxLength(30), RegularExpression(@"^\+?[0-9 \-]{6,30}$", ErrorMessage = "ContactPhone may contain digits, spaces, '-' and a leading '+'.")]
    public string? ContactPhone { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }
}
