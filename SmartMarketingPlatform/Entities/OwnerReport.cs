namespace SmartMarketingPlatform.Entities;

// A periodic summary for the owner: the numbers of the period (facts) and the AI's reading of them.
public class OwnerReport
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string FactsJson { get; set; } = "{}";
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string WinsJson { get; set; } = "[]";
    public string ConcernsJson { get; set; } = "[]";
    public string ActionsJson { get; set; } = "[]";
    public Guid? GeneratedByExecutionId { get; set; }
    public bool IsScheduled { get; set; }                      // made by the weekly worker, not on request
    public DateTimeOffset? SentAt { get; set; }
    public string? SentTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public AiExecution? GeneratedByExecution { get; set; }
}
