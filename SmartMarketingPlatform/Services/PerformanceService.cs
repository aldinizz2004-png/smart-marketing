using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Publishing;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Results: what was published and how it performed. The basis for "learn from results".
public sealed class PerformanceService(
    MarketingDbContext db,
    IBusinessAccess access,
    IPublicationProcessor processor,
    TimeProvider clock) : IPerformanceService
{
    // Object-initializer projection (not a constructor), so EF can still filter and sort on it in SQL.
    private sealed class Row
    {
        public Guid Id { get; init; }
        public Guid JobId { get; init; }
        public Guid ContentItemId { get; init; }
        public string Platform { get; init; } = string.Empty;
        public string? ExternalPostId { get; init; }
        public string? ExternalUrl { get; init; }
        public DateTimeOffset PublishedAt { get; init; }
        public ContentFormat Format { get; init; }
        public HookType? Hook { get; init; }
    }

    public async Task<PagedResult<PublicationResponse>> GetPublicationsAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var page = await Rows(businessId)
            .OrderByDescending(r => r.PublishedAt).ThenByDescending(r => r.Id)
            .ToPagedResultAsync(query, cancellationToken);

        var latest = await LatestAsync(page.Items.Select(r => r.Id).ToList(), cancellationToken);
        return page.Map(r =>
        {
            var found = latest.TryGetValue(r.Id, out var m);
            return new PublicationResponse(r.Id, r.JobId, r.ContentItemId, r.Platform, r.ExternalPostId, r.ExternalUrl, r.PublishedAt,
                found ? m.Values : new Dictionary<MetricCode, decimal>(), found ? m.At : null);
        });
    }

    public async Task<IReadOnlyList<MetricPoint>> GetMetricHistoryAsync(Guid businessId, Guid publicationId, CancellationToken cancellationToken)
    {
        await EnsurePublicationAsync(businessId, publicationId, cancellationToken);
        return await db.MetricObservations.AsNoTracking()
            .Where(m => m.PublicationId == publicationId)
            .OrderBy(m => m.ObservedAt).ThenBy(m => m.MetricCode)
            .Select(m => new MetricPoint(m.MetricCode, m.MetricValue, m.ObservedAt))
            .ToListAsync(cancellationToken);
    }

    // "Refresh now" button: one fresh snapshot for this publication.
    public async Task<IReadOnlyList<MetricPoint>> RefreshMetricsAsync(Guid businessId, Guid publicationId, CancellationToken cancellationToken)
    {
        await EnsurePublicationAsync(businessId, publicationId, cancellationToken);
        await processor.CollectMetricsAsync(publicationId, cancellationToken);
        return await GetMetricHistoryAsync(businessId, publicationId, cancellationToken);
    }

    public async Task<PerformanceResponse> GetPerformanceAsync(Guid businessId, PerformanceQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var to = query.To ?? clock.GetUtcNow();
        var from = query.From ?? to.AddDays(-30);
        if (from >= to) throw new BadRequestException("'from' must be before 'to'.");

        var rows = await Rows(businessId).Where(r => r.PublishedAt >= from && r.PublishedAt < to).ToListAsync(cancellationToken);
        var latest = await LatestAsync(rows.Select(r => r.Id).ToList(), cancellationToken);

        decimal V(Guid id, MetricCode code) => latest.TryGetValue(id, out var m) && m.Values.TryGetValue(code, out var v) ? v : 0;
        decimal Engagement(Guid id) => V(id, MetricCode.Likes) + V(id, MetricCode.Comments) + V(id, MetricCode.Shares) + V(id, MetricCode.Saves);
        decimal Rate(decimal engagement, decimal reach) => reach == 0 ? 0 : Math.Round(engagement / reach, 4);

        IReadOnlyList<PerformanceGroup> Group(Func<Row, string> key) => rows
            .GroupBy(key)
            .Select(g => new PerformanceGroup(
                g.Key,
                g.Count(),
                Math.Round(g.Average(r => V(r.Id, MetricCode.Reach)), 1),
                Rate(g.Sum(r => Engagement(r.Id)), g.Sum(r => V(r.Id, MetricCode.Reach)))))
            .OrderByDescending(g => g.EngagementRate).ThenByDescending(g => g.AverageReach)
            .ToList();

        var totalReach = rows.Sum(r => V(r.Id, MetricCode.Reach));
        var totals = new PerformanceTotals(
            totalReach,
            rows.Sum(r => V(r.Id, MetricCode.Impressions)),
            rows.Sum(r => V(r.Id, MetricCode.Likes)),
            rows.Sum(r => V(r.Id, MetricCode.Comments)),
            rows.Sum(r => V(r.Id, MetricCode.Shares)),
            rows.Sum(r => V(r.Id, MetricCode.Saves)),
            rows.Sum(r => V(r.Id, MetricCode.LinkClicks)),
            Rate(rows.Sum(r => Engagement(r.Id)), totalReach));

        var top = rows
            .Select(r => new TopPost(r.Id, r.ContentItemId, r.Platform, r.Format, r.Hook, V(r.Id, MetricCode.Reach),
                Rate(Engagement(r.Id), V(r.Id, MetricCode.Reach)), r.ExternalUrl))
            .OrderByDescending(t => t.Reach).Take(3).ToList();

        return new PerformanceResponse(from, to, rows.Count, totals,
            Group(r => r.Platform), Group(r => r.Format.ToString()), Group(r => r.Hook?.ToString() ?? "None"), top);
    }

    private IQueryable<Row> Rows(Guid businessId) =>
        db.Publications.AsNoTracking()
            .Where(p => p.PublicationJob.ContentItem.Campaign.BusinessId == businessId)
            .Select(p => new Row
            {
                Id = p.Id,
                JobId = p.PublicationJobId,
                ContentItemId = p.PublicationJob.ContentItemId,
                Platform = p.PublicationJob.SocialAccount.PlatformCode,
                ExternalPostId = p.ExternalPostId,
                ExternalUrl = p.ExternalUrl,
                PublishedAt = p.PublishedAt,
                Format = p.PublicationJob.Revision.Format,
                Hook = p.PublicationJob.Revision.HookType
            });

    private async Task EnsurePublicationAsync(Guid businessId, Guid publicationId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        if (!await db.Publications.AnyAsync(p => p.Id == publicationId && p.PublicationJob.ContentItem.Campaign.BusinessId == businessId, cancellationToken))
            throw new NotFoundException("Publication not found.");
    }

    // Latest snapshot of each publication (all metric codes share the same ObservedAt).
    private async Task<Dictionary<Guid, (Dictionary<MetricCode, decimal> Values, DateTimeOffset At)>> LatestAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        var rows = await db.MetricObservations.AsNoTracking()
            .Where(m => ids.Contains(m.PublicationId)
                        && m.ObservedAt == db.MetricObservations.Where(x => x.PublicationId == m.PublicationId).Max(x => x.ObservedAt))
            .ToListAsync(cancellationToken);
        return rows.GroupBy(r => r.PublicationId)
            .ToDictionary(g => g.Key, g => (g.ToDictionary(r => r.MetricCode, r => r.MetricValue), g.First().ObservedAt));
    }
}
