namespace SmartMarketingPlatform.Entities;

public class AudienceSegment
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;            // e.g. "University students"
    public string? Description { get; set; }
    public string? TargetingRulesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
}
