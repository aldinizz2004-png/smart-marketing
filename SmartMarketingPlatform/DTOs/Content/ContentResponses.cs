using System.Text.Json;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Content;

// Lists return the latest revision only; GET by id returns every revision with its reviews.
public sealed record ContentItemResponse(
    Guid Id,
    Guid BusinessId,
    Guid CampaignId,
    string PlatformCode,
    ContentType ContentType,
    StatusInfo Status,
    DateTimeOffset? PlannedPublishAt,
    string? VariantLabel,
    Guid? SourceRecommendationId,
    ContentRevisionResponse? LatestRevision,
    IReadOnlyList<ContentRevisionResponse>? Revisions,
    DateTimeOffset CreatedAt);

public sealed record ContentRevisionResponse(
    Guid Id,
    int RevisionNo,
    string Body,
    ContentFormat Format,
    HookType? HookType,
    CallToAction? CallToAction,
    bool IncludesOffer,
    JsonElement? CreativeDetails,
    string Author,                       // "ai" or "user"
    Guid? CreatedBy,
    Guid? GeneratedByExecutionId,
    IReadOnlyList<RevisionAssetResponse> Assets,
    IReadOnlyList<ContentReviewResponse> Reviews,
    DateTimeOffset CreatedAt);

public sealed record RevisionAssetResponse(Guid AssetId, byte Position, string MimeType, string ContentUrl);

public sealed record ContentReviewResponse(Guid Id, Guid ReviewerId, StatusInfo Decision, string? Feedback, DateTimeOffset DecidedAt);
