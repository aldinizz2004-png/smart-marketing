using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Content;

// POST .../reviews { "decision": "Approved" | "ChangesRequested" | "Rejected", "feedback": "..." }
public sealed class ReviewRequest
{
    [Required, MaxLength(30)]
    public string? Decision { get; init; }

    [MaxLength(2000)]
    public string? Feedback { get; init; }                      // required unless Approved
}
