using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Auth;

// Body of POST /api/auth/refresh and POST /api/auth/logout.
// Browsers using the cookie (X-Auth-Transport: cookie) send no body at all.
public sealed class RefreshTokenRequest
{
    [MaxLength(200)]
    public string? RefreshToken { get; init; }
}
