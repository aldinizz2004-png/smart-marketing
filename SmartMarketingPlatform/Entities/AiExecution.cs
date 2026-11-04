using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Every call to an AI model: kept for traceability, debugging and cost monitoring.
public class AiExecution
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public AiTaskType TaskType { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public int StatusId { get; set; }                           // Statuses (Category = "AiExecution")
    public string? InputSnapshotJson { get; set; }              // what we sent (for debugging)
    public string? OutputSnapshotJson { get; set; }             // what came back
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Status Status { get; set; } = null!;
}
