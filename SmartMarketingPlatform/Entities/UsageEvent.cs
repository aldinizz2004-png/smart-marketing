using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Usage and cost ledger per tenant (cost monitoring).
public class UsageEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? AiExecutionId { get; set; }
    public UsageEventType EventType { get; set; }
    public decimal Units { get; set; }                          // e.g. tokens, images, posts
    public decimal EstimatedCostUsd { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;  // the same event is never counted twice
    public DateTimeOffset OccurredAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Business? Business { get; set; }
    public AiExecution? AiExecution { get; set; }
}
