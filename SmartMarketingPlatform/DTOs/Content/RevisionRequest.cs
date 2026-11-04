using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Content;

// A new version of the content. Either a person writes it (text + format),
// or the AI writes it (generateWithAi = true, optionally with productId / offerId / instructions).
public sealed class RevisionRequest
{
    public bool GenerateWithAi { get; init; }

    [MaxLength(5000)]
    public string? Text { get; init; }

    [EnumDataType(typeof(ContentFormat))]
    public ContentFormat? Format { get; init; }

    [EnumDataType(typeof(HookType))]
    public HookType? HookType { get; init; }

    [EnumDataType(typeof(CallToAction))]
    public CallToAction? CallToAction { get; init; }

    public bool? IncludesOffer { get; init; }

    [MaxLength(10)]
    public List<Guid> AssetIds { get; init; } = [];             // order = position in the post

    // Only for the AI:
    public Guid? ProductId { get; init; }
    public Guid? OfferId { get; init; }

    [MaxLength(500)]
    public string? Instructions { get; init; }                  // e.g. "make it shorter"
}
