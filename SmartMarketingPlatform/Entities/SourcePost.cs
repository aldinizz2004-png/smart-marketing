using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A public post collected from a tracked source, with its engagement at capture time.
// It uses the same Format / HookType as our own content so the two can be compared.
public class SourcePost
{
    public Guid Id { get; set; }
    public Guid TrackedSourceId { get; set; }
    public string? ExternalPostId { get; set; }
    public string? PostUrl { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public ContentFormat Format { get; set; }
    public HookType? HookType { get; set; }
    public string? CaptionText { get; set; }
    public string? HashtagsJson { get; set; }
    public int? LikesCount { get; set; }
    public int? CommentsCount { get; set; }
    public int? SharesCount { get; set; }
    public long? ViewsCount { get; set; }
    public decimal? EngagementRate { get; set; }
    public DateTimeOffset CapturedAt { get; set; }

    public TrackedSource TrackedSource { get; set; } = null!;
}
