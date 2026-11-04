using System.Text.Json;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Usage;

// What the AI cost this tenant in a period: the basis for pricing and limits.
public sealed record AiUsageResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal TotalCostUsd,
    AiExecutionStats Executions,
    IReadOnlyList<UsageByType> ByEventType,
    IReadOnlyList<UsageByBusiness> ByBusiness);

public sealed record AiExecutionStats(int Total, int Succeeded, int Failed, int? AverageLatencyMs);
public sealed record UsageByType(UsageEventType EventType, int Events, decimal Units, decimal CostUsd);
public sealed record UsageByBusiness(Guid BusinessId, string BusinessName, int Events, decimal CostUsd);

// One AI call, with what was sent and what came back (for debugging and explaining answers).
public sealed record AiExecutionResponse(
    Guid Id,
    Guid BusinessId,
    AiTaskType TaskType,
    string ModelName,
    string PromptVersion,
    StatusInfo Status,
    JsonElement? Input,
    JsonElement? Output,
    int? InputTokens,
    int? OutputTokens,
    decimal? EstimatedCostUsd,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
