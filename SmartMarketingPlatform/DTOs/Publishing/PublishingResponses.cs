using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Publishing;

public sealed record PublicationJobResponse(
    Guid Id,
    Guid ContentItemId,
    Guid RevisionId,
    Guid SocialAccountId,
    string PlatformCode,
    DateTimeOffset ScheduledAt,
    StatusInfo Status,
    int AttemptCount,
    string? LastError,
    PublicationResponse? Publication,
    DateTimeOffset CreatedAt);

public sealed record PublicationResponse(
    Guid Id,
    Guid PublicationJobId,
    Guid ContentItemId,
    string PlatformCode,
    string? ExternalPostId,
    string? ExternalUrl,
    DateTimeOffset PublishedAt,
    IReadOnlyDictionary<MetricCode, decimal> LatestMetrics,
    DateTimeOffset? MetricsObservedAt);

public sealed record MetricPoint(MetricCode MetricCode, decimal Value, DateTimeOffset ObservedAt);

// "What works for us": the basis for the next strategy run.
public sealed record PerformanceResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int Posts,
    PerformanceTotals Totals,
    IReadOnlyList<PerformanceGroup> ByPlatform,
    IReadOnlyList<PerformanceGroup> ByFormat,
    IReadOnlyList<PerformanceGroup> ByHook,
    IReadOnlyList<TopPost> TopPosts);

public sealed record PerformanceTotals(decimal Reach, decimal Impressions, decimal Likes, decimal Comments,
    decimal Shares, decimal Saves, decimal LinkClicks, decimal EngagementRate);

public sealed record PerformanceGroup(string Key, int Posts, decimal AverageReach, decimal EngagementRate);

public sealed record TopPost(Guid PublicationId, Guid ContentItemId, string PlatformCode, ContentFormat Format,
    HookType? HookType, decimal Reach, decimal EngagementRate, string? ExternalUrl);
