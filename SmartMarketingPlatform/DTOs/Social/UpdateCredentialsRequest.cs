using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Social;

public sealed class UpdateCredentialsRequest
{
    [Required, MaxLength(4000)]
    public string? AccessToken { get; init; }
}
