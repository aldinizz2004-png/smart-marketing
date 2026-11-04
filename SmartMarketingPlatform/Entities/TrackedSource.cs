using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Any URL the owner pastes: a competitor, a professional account or an inspiration.
// The platform is detected from the URL host through PlatformDomains ("website" if unknown).
public class TrackedSource
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Url { get; set; } = string.Empty;             // as the owner pasted it
    public string NormalizedUrl { get; set; } = string.Empty;   // cleaned, used to avoid duplicates
    public string Host { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;    // FK -> Platform
    public TrackedSourceKind Kind { get; set; }
    public string? DisplayName { get; set; }
    public string? Handle { get; set; }
    public string? Notes { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "TrackedSource")
    public DateTimeOffset? LastScannedAt { get; set; }
    public Guid AddedBy { get; set; }                           // FK -> AppUser
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Platform Platform { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public AppUser AddedByUser { get; set; } = null!;

    public ICollection<SourcePost> Posts { get; set; } = new List<SourcePost>();
    public ICollection<CompetitorInsight> Insights { get; set; } = new List<CompetitorInsight>();
}
