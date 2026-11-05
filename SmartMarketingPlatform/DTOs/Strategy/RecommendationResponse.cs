using System.Text.Json;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Strategy;

public sealed record RecommendationResponse(
    Guid Id,
    Guid StrategyRunId,
    Guid? CampaignId,
    RecommendationAction ActionType,
    JsonElement ProposedPayload,
    DateTimeOffset? ProposedPublishAt,
    string Rationale,
    decimal? Confidence,
    StatusInfo Status,
    bool AppliedAutomatically,
    Guid? DecidedBy,
    DateTimeOffset? DecidedAt,
    DateTimeOffset? AppliedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RecommendationEvidenceResponse> Evidence);

// What the recommendation is based on: a competitor insight, a competitor post or one of our metrics.
public sealed record RecommendationEvidenceResponse(Guid? CompetitorInsightId, Guid? SourcePostId, Guid? MetricObservationId, string? Interpretation);
