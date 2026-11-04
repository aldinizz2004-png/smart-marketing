using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserSummary User,
    TenantSummary Tenant);

public sealed record UserSummary(Guid Id, string Email, string? FullName, string PreferredLanguage);

public sealed record TenantSummary(Guid Id, string Name, MembershipRole Role);
