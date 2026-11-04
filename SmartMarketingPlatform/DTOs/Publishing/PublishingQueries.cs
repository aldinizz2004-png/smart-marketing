using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Publishing;

// GET .../publication-jobs?status=Queued
public sealed class JobListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }
}

// GET .../performance?from=...&to=...   (default: last 30 days)
public sealed class PerformanceQuery
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
