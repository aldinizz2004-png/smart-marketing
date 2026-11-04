using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [Required, MaxLength(100)]
    public string? Password { get; init; }

    // Optional: which tenant to work on when the user belongs to several.
    // Empty = the oldest active membership.
    public Guid? TenantId { get; init; }
}
