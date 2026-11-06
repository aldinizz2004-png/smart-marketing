using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Auth;

// AccessToken goes in the Authorization header and expires quickly. RefreshToken is kept safely by
// the client and sent to POST /api/auth/refresh for a new pair; each refresh token works once.
// RefreshToken is null when it was set as an httpOnly cookie instead (X-Auth-Transport: cookie).
public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    string? RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User,
    TenantSummary Tenant);

public sealed record UserSummary(Guid Id, string Email, string? FullName, string PreferredLanguage);

public sealed record TenantSummary(Guid Id, string Name, MembershipRole Role);

public sealed record LogoutAllResponse(int SessionsEnded);
