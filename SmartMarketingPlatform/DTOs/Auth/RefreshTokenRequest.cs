using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Auth;

// Body of POST /api/auth/refresh and POST /api/auth/logout.
public sealed class RefreshTokenRequest
{
    [Required, MaxLength(200)]
    public string? RefreshToken { get; init; }
}
