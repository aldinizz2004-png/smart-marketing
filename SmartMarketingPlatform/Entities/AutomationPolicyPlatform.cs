namespace SmartMarketingPlatform.Entities;

// Join entity: AutomationPolicy <-> Platform (the allowed platforms).
public class AutomationPolicyPlatform
{
    public Guid BusinessId { get; set; }                        // FK -> AutomationPolicy
    public string PlatformCode { get; set; } = string.Empty;    // FK -> Platform

    public AutomationPolicy Policy { get; set; } = null!;
    public Platform Platform { get; set; } = null!;
}
