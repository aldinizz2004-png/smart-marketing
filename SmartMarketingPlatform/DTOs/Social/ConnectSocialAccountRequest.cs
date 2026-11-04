using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Social;

// Manual connection for now; later this is filled by the platform's OAuth callback.
public sealed class ConnectSocialAccountRequest
{
    [Required, MaxLength(30)]
    public string? PlatformCode { get; init; }                  // e.g. "instagram"

    [Required, MaxLength(200)]
    public string? ExternalAccountId { get; init; }             // the page/account id on that platform

    [MaxLength(100)]
    public string? Handle { get; init; }

    [MaxLength(4000)]
    public string? AccessToken { get; init; }                   // stored encrypted, never returned
}
