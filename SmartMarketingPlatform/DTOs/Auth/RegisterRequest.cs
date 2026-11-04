using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Auth;

// Sign-up of a shop owner: creates the user, the tenant and the Owner membership together.
public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one digit.")]
    public string? Password { get; init; }

    [Required, MaxLength(200)]
    public string? FullName { get; init; }

    [Required, MaxLength(200)]
    public string? TenantName { get; init; }

    [RegularExpression("^(ar|en)$", ErrorMessage = "PreferredLanguage must be 'ar' or 'en'.")]
    public string? PreferredLanguage { get; init; }
}
