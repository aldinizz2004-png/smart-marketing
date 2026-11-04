namespace SmartMarketingPlatform.Entities;

// One analysis of a results window (our own results + competitor data).
public class StrategyRun
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public int StatusId { get; set; }                           // Statuses (Category = "StrategyRun")
    public string? InputSummaryJson { get; set; }
    public Guid? AiExecutionId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public AiExecution? AiExecution { get; set; }
    public ICollection<StrategyRecommendation> Recommendations { get; set; } = new List<StrategyRecommendation>();
}
