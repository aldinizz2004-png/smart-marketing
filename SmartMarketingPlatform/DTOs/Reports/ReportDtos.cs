using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Ai;

namespace SmartMarketingPlatform.DTOs.Reports;

public sealed class GenerateReportRequest
{
    [Range(1, 31)]
    public int Days { get; init; } = 7;

    public bool Send { get; init; }
}

public sealed record ReportSummaryResponse(
    Guid Id, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, string Headline, bool IsScheduled, DateTimeOffset? SentAt, DateTimeOffset CreatedAt);

public sealed record ReportResponse(
    Guid Id,
    Guid BusinessId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string Headline,
    string Summary,
    IReadOnlyList<string> Wins,
    IReadOnlyList<string> Concerns,
    IReadOnlyList<string> NextActions,
    OwnerReportFacts Facts,
    Guid? GeneratedByExecutionId,
    bool IsScheduled,
    DateTimeOffset? SentAt,
    string? SentTo,
    DateTimeOffset CreatedAt);
