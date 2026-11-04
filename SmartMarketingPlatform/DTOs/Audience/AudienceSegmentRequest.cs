using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SmartMarketingPlatform.DTOs.Audience;

// Used by POST (create) and PUT (full replace).
public sealed class AudienceSegmentRequest
{
    [Required, MaxLength(150)]
    public string? Name { get; init; }                          // e.g. "University students"

    [MaxLength(1000)]
    public string? Description { get; init; }

    // Free-form JSON object, e.g. { "ageMin": 18, "ageMax": 25, "cities": ["Nablus"] }
    public JsonElement? TargetingRules { get; init; }
}
