using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Social;

public sealed class ConnectLinkRequest
{
    [Required, MaxLength(30)]
    public string? PlatformCode { get; init; }
}

public sealed record ConnectLinkResponse(string PlatformCode, string AuthorizationUrl, DateTimeOffset ExpiresAt);

public sealed record ConnectionResult(bool Success, string? PlatformCode, Guid? SocialAccountId, string? Handle, string? Error);
