using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// What the AI learned from tracked sources, e.g. "reels posted 20:00-22:00 get 3x more engagement".
// TrackedSourceId = null means the insight comes from all tracked sources together.
public class CompetitorInsight
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? TrackedSourceId { get; set; }
    public InsightType InsightType { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public decimal? Confidence { get; set; }                    // 0 .. 1
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public TrackedSource? TrackedSource { get; set; }
}
