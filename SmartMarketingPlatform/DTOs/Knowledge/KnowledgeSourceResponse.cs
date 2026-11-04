using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Knowledge;

// Lists return only a preview of the text; GET by id returns the full ExtractedText.
public sealed record KnowledgeSourceResponse(
    Guid Id,
    Guid BusinessId,
    KnowledgeSourceType SourceType,
    string? SourceUrl,
    Guid? AssetId,
    StatusInfo Status,
    int TextLength,
    string? TextPreview,
    string? ExtractedText,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset CreatedAt);
