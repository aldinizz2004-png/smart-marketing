namespace SmartMarketingPlatform.DTOs.Usage;

// GET /api/tenant/ai-usage?from=2026-10-01&to=2026-11-01 (default: this month so far)
public sealed class AiUsageQuery
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
