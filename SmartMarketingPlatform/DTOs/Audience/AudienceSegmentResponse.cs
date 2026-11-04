using System.Text.Json;

namespace SmartMarketingPlatform.DTOs.Audience;

public sealed record AudienceSegmentResponse(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Description,
    JsonElement? TargetingRules,
    DateTimeOffset CreatedAt);
