using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Competitors;

// POST .../tracked-sources { "url": "https://instagram.com/some_cafe", "kind": "Competitor" }
public sealed class TrackedSourceRequest
{
    [Required, MaxLength(2048)]
    public string? Url { get; init; }

    [Required]
    public TrackedSourceKind? Kind { get; init; }

    [MaxLength(200)]
    public string? DisplayName { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

// GET .../tracked-sources?status=Active&kind=Competitor&platform=instagram
public sealed class TrackedSourceListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    public TrackedSourceKind? Kind { get; init; }

    [MaxLength(30)]
    public string? Platform { get; init; }
}

public sealed record TrackedSourceResponse(
    Guid Id,
    string Url,
    string Platform,
    string? Handle,
    string? DisplayName,
    TrackedSourceKind Kind,
    string? Notes,
    StatusInfo Status,
    int Posts,
    DateTimeOffset? LastScannedAt,
    DateTimeOffset CreatedAt);

public sealed record SourcePostResponse(
    Guid Id,
    string? PostUrl,
    DateTimeOffset? PostedAt,
    ContentFormat Format,
    HookType? HookType,
    string? Caption,
    IReadOnlyList<string> Hashtags,
    int? Likes,
    int? Comments,
    long? Views,
    decimal? EngagementRate,
    DateTimeOffset CapturedAt);

// Error is set when the scan could not read the account (the source is then Failed).
public sealed record ScanResponse(Guid TrackedSourceId, int Fetched, int Added, int Updated, StatusInfo Status, string? Error);

public sealed record CompetitorInsightResponse(
    Guid Id,
    Guid? TrackedSourceId,
    InsightType Type,
    string Summary,
    string? ApplyToUs,
    decimal? Confidence,
    IReadOnlyList<Guid> EvidencePostIds,
    DateTimeOffset CreatedAt);

public sealed record InsightAnalysisResponse(int Sources, int PostsAnalyzed, Guid ExecutionId, IReadOnlyList<CompetitorInsightResponse> Insights);
