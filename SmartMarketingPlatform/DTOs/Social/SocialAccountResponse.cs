using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.DTOs.Social;

// The token and its storage reference are never part of a response.
public sealed record SocialAccountResponse(
    Guid Id,
    Guid BusinessId,
    string PlatformCode,
    string ExternalAccountId,
    string? Handle,
    bool HasCredential,
    StatusInfo Status,
    DateTimeOffset CreatedAt);
