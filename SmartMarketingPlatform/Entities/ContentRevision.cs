using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A version of the text and creative of a content item (revision 1, 2, 3 ...).
public class ContentRevision
{
    public Guid Id { get; set; }
    public Guid ContentItemId { get; set; }
    public int RevisionNo { get; set; }
    public string Body { get; set; } = string.Empty;

    // Explicit attributes used to compare results (ours vs competitors).
    public ContentFormat Format { get; set; }
    public HookType? HookType { get; set; }
    public CallToAction? CallToAction { get; set; }
    public bool IncludesOffer { get; set; }

    public string? CreativeDetailsJson { get; set; }
    public string? GenerationContextJson { get; set; }          // prices / brand rules used by the AI
    public Guid? GeneratedByExecutionId { get; set; }           // FK -> AiExecution (null = written by a person)
    public Guid? SourceRecommendationId { get; set; }
    public Guid? CreatedBy { get; set; }                        // FK -> AppUser (null = written by the AI)
    public DateTimeOffset CreatedAt { get; set; }

    public ContentItem ContentItem { get; set; } = null!;
    public AiExecution? GeneratedByExecution { get; set; }
    public StrategyRecommendation? SourceRecommendation { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public ICollection<ContentRevisionAsset> Assets { get; set; } = new List<ContentRevisionAsset>();
    public ICollection<ContentReview> Reviews { get; set; } = new List<ContentReview>();
}
