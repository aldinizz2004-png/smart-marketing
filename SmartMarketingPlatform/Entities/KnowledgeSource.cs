using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Menus, websites, past posts ... that the AI reads to learn the business.
public class KnowledgeSource
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? AssetId { get; set; }                          // uploaded file, or ...
    public string? SourceUrl { get; set; }                      // ... a link
    public KnowledgeSourceType SourceType { get; set; }
    public string? ExtractedText { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "KnowledgeSource")
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public MediaAsset? Asset { get; set; }
    public Status Status { get; set; } = null!;
}
