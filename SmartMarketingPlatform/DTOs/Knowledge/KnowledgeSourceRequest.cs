using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Knowledge;

// Something the AI should learn about the business. Send exactly ONE of:
// sourceUrl (a website/menu page), assetId (an uploaded file) or text (pasted text).
public sealed class KnowledgeSourceRequest
{
    [Required, EnumDataType(typeof(KnowledgeSourceType))]
    public KnowledgeSourceType? SourceType { get; init; }

    [MaxLength(2048)]
    public string? SourceUrl { get; init; }

    public Guid? AssetId { get; init; }

    [MaxLength(50_000)]
    public string? Text { get; init; }
}
