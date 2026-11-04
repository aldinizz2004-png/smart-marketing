using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Common;

// PATCH .../status  { "status": "Paused" }  (the status Code, see /api/statuses/{category})
public sealed class ChangeStatusRequest
{
    [Required, MaxLength(30)]
    public string? Status { get; init; }
}
