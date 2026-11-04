namespace SmartMarketingPlatform.Entities;

// Social platforms are data: a new network is a new row, not a code change.
public class Platform
{
    public string Code { get; set; } = string.Empty;            // primary key, e.g. "instagram"
    public string DisplayNameKey { get; set; } = string.Empty;  // e.g. "platform.instagram"
    public bool SupportsPublishing { get; set; }
    public bool SupportsMessaging { get; set; }
    public bool SupportsCompetitorTracking { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PlatformDomain> Domains { get; set; } = new List<PlatformDomain>();
}