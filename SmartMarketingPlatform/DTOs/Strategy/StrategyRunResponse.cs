using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.DTOs.Strategy;

public sealed record StrategyRunResponse(
    Guid Id,
    Guid BusinessId,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    string AlgorithmVersion,
    StatusInfo Status,
    Guid? AiExecutionId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<RecommendationResponse> Recommendations);
