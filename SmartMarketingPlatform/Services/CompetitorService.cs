using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Competitors;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// The owner pastes the link of an account to learn from. Its public posts are collected (scan),
// code computes the patterns (formats, hooks, times, frequency), and the AI turns them into advice
// for OUR products. Their captions are never reused: the copy guard blocks texts that are too close.
public sealed class CompetitorService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    PlatformDetector detector,
    ICompetitorSource source,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    StatusCatalog statuses,
    IOptions<CompetitorOptions> options,
    TimeProvider clock,
    ILogger<CompetitorService> logger) : ICompetitorService
{
    private const int MaxPostsPerAnalysis = 300;

    // Paths that are a post or a page of the platform, not an account.
    private static readonly HashSet<string> NotAccounts = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "reel", "reels", "stories", "explore", "tv", "watch", "share", "groups", "hashtag", "search", "video", "photo", "permalink.php"
    };

    // ---- tracked sources --------------------------------------------------------------

    public async Task<PagedResult<TrackedSourceResponse>> GetSourcesAsync(Guid businessId, TrackedSourceListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var sources = db.TrackedSources.AsNoTracking().Where(s => s.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("TrackedSource", query.Status);
            sources = sources.Where(s => s.StatusId == statusId);
        }
        if (query.Kind is { } kind)
            sources = sources.Where(s => s.Kind == kind);
        if (!string.IsNullOrWhiteSpace(query.Platform))
            sources = sources.Where(s => s.PlatformCode == query.Platform.ToLower());

        var page = await Project(sources.OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id)).ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<TrackedSourceResponse> GetSourceAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    public async Task<TrackedSourceResponse> AddSourceAsync(Guid businessId, TrackedSourceRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var detected = await detector.DetectAsync(request.Url!, cancellationToken);
        if (detected.PlatformCode == PlatformDetector.FallbackPlatform || detected.Handle is null)
            throw new BadRequestException("Paste the link of a social media account, e.g. https://instagram.com/account_name.");
        if (NotAccounts.Contains(detected.Handle))
            throw new BadRequestException("This is the link of a post or a page. Paste the link of the account itself.");
        if (!source.Supports(detected.PlatformCode))
            throw new BadRequestException($"Reading {detected.PlatformCode} accounts is not available yet. Supported now: Instagram.");

        // One canonical address per account, whatever was pasted (?igsh=..., /reels, upper case ...).
        var handle = detected.Handle.ToLowerInvariant();
        var normalized = $"https://{detected.Host}/{handle}";
        if (await db.TrackedSources.AnyAsync(s => s.BusinessId == businessId && s.NormalizedUrl == normalized, cancellationToken))
            throw new ConflictException("This account is already tracked.");

        var tracked = new TrackedSource
        {
            BusinessId = businessId,
            Url = request.Url!.Trim(),
            NormalizedUrl = normalized,
            Host = detected.Host,
            PlatformCode = detected.PlatformCode,
            Kind = request.Kind!.Value,
            DisplayName = request.DisplayName?.Trim(),
            Handle = Truncate(handle, 100),
            Notes = request.Notes?.Trim(),
            StatusId = StatusIds.TrackedSource.Pending,
            AddedBy = currentUser.UserId,
            CreatedAt = clock.GetUtcNow()
        };
        db.TrackedSources.Add(tracked);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("This account is already tracked.");
        }

        // First scan right away, so the owner sees posts at once. A temporary failure leaves it Pending
        // for the background worker; an unreadable account becomes Failed.
        await ScanCoreAsync(tracked, cancellationToken);
        return await LoadResponseAsync(businessId, tracked.Id, cancellationToken);
    }

    // PATCH .../status { "status": "Paused" | "Active" }. Active resumes (or retries a Failed source).
    public async Task<TrackedSourceResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var tracked = await LoadAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("TrackedSource", request.Status!);
        if (target == StatusIds.TrackedSource.Paused)
            tracked.StatusId = StatusIds.TrackedSource.Paused;
        else if (target == StatusIds.TrackedSource.Active)
        {
            if (tracked.StatusId is StatusIds.TrackedSource.Paused or StatusIds.TrackedSource.Failed)
                tracked.StatusId = tracked.StatusId == StatusIds.TrackedSource.Paused && tracked.LastScannedAt is not null
                    ? StatusIds.TrackedSource.Active
                    : StatusIds.TrackedSource.Pending;
        }
        else
            throw new BadRequestException("A tracked source can only be set to 'Active' or 'Paused'.");

        await db.SaveChangesAsync(cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    // Collected posts and insights go with the source, unless a recommendation cites them as evidence.
    public async Task DeleteSourceAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var tracked = await LoadAsync(businessId, id, cancellationToken);

        var cited = await db.RecommendationEvidence.AnyAsync(e =>
            (e.SourcePost != null && e.SourcePost.TrackedSourceId == id) ||
            (e.CompetitorInsight != null && e.CompetitorInsight.TrackedSourceId == id), cancellationToken);
        if (cited)
            throw new ConflictException("Recommendations are based on this source. Pause it instead of deleting it.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.CompetitorInsights.Where(i => i.TrackedSourceId == id).ExecuteDeleteAsync(cancellationToken);
        await db.SourcePosts.Where(p => p.TrackedSourceId == id).ExecuteDeleteAsync(cancellationToken);
        db.TrackedSources.Remove(tracked);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ScanResponse> ScanAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var tracked = await LoadAsync(businessId, id, cancellationToken);
        if (tracked.StatusId == StatusIds.TrackedSource.Paused)
            throw new ConflictException("This source is paused. Set it to Active first.");

        var outcome = await ScanCoreAsync(tracked, cancellationToken);
        if (outcome.Error is not null && !outcome.SourceProblem)
            throw new BadGatewayException($"The account could not be read right now: {outcome.Error}");

        return new ScanResponse(tracked.Id, outcome.Fetched, outcome.Added, outcome.Updated, statuses.Describe(tracked.StatusId), outcome.Error);
    }

    public async Task<PagedResult<SourcePostResponse>> GetPostsAsync(Guid businessId, Guid id, PageQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        if (!await db.TrackedSources.AnyAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken))
            throw new NotFoundException("Tracked source not found.");

        var page = await db.SourcePosts.AsNoTracking()
            .Where(p => p.TrackedSourceId == id)
            .OrderByDescending(p => p.PostedAt).ThenBy(p => p.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(p => new SourcePostResponse(
            p.Id, p.PostUrl, p.PostedAt, p.Format, p.HookType, p.CaptionText, Hashtags(p.HashtagsJson),
            p.LikesCount, p.CommentsCount, p.ViewsCount, p.EngagementRate, p.CapturedAt));
    }

    // ---- insights ---------------------------------------------------------------------

    public async Task<InsightAnalysisResponse> AnalyzeAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        return await AnalyzeCoreAsync(businessId, cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitorInsightResponse>> GetLatestInsightsAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var latest = await LatestAnalysisAsync(businessId, cancellationToken);
        if (latest is null) return [];

        var insights = await db.CompetitorInsights.AsNoTracking()
            .Where(i => i.BusinessId == businessId && i.CreatedAt == latest)
            .OrderByDescending(i => i.Confidence)
            .ToListAsync(cancellationToken);
        return insights.Select(ToResponse).ToList();
    }

    private async Task<InsightAnalysisResponse> AnalyzeCoreAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var since = clock.GetUtcNow().AddDays(-settings.AnalysisWindowDays);

        var posts = await db.SourcePosts.AsNoTracking()
            .Where(p => p.TrackedSource.BusinessId == businessId
                        && p.TrackedSource.StatusId == StatusIds.TrackedSource.Active
                        && (p.PostedAt ?? p.CapturedAt) >= since)
            .OrderByDescending(p => p.PostedAt)
            .Take(MaxPostsPerAnalysis)
            .Select(p => new StatPost(
                p.Id, p.TrackedSourceId, p.TrackedSource.DisplayName ?? p.TrackedSource.Handle ?? p.TrackedSource.Host,
                p.Format, p.HookType, p.PostedAt, p.CaptionText, p.EngagementRate,
                (p.LikesCount ?? 0) + (p.CommentsCount ?? 0)))
            .ToListAsync(cancellationToken);
        if (posts.Count < settings.MinPostsForAnalysis)
            throw new ConflictException(
                $"At least {settings.MinPostsForAnalysis} posts from active tracked sources are needed (found {posts.Count}). Add or scan sources first.");

        var context = await contextBuilder.BuildAsync(businessId, null, cancellationToken);
        var facts = CompetitorStats.Build(posts, TimeZoneInfo.FindSystemTimeZoneById(context.TimeZoneId));

        var (drafts, execution) = await aiRunner.RunAsync(
            businessId, AiTaskType.CompetitorAnalysis, UsageEventType.CompetitorAnalysis, new { facts },
            ct => ai.AnalyzeCompetitorsAsync(context, facts, ct), cancellationToken);

        // One timestamp for the whole batch: "the latest analysis" is every insight created at that moment.
        var createdAt = clock.GetUtcNow();
        var sourceOf = posts.ToDictionary(p => p.Id, p => p.SourceId);
        var saved = new List<CompetitorInsight>();
        foreach (var draft in drafts)
        {
            var evidenceSources = draft.EvidencePostIds.Where(sourceOf.ContainsKey).Select(id => sourceOf[id]).Distinct().ToList();
            var insight = new CompetitorInsight
            {
                BusinessId = businessId,
                TrackedSourceId = facts.Sources == 1 ? posts[0].SourceId : evidenceSources.Count == 1 ? evidenceSources[0] : null,
                InsightType = draft.Type,
                Summary = Truncate(draft.Summary, 1000)!,
                DetailsJson = new CompetitorInsightDetails(draft.ApplyToUs, draft.EvidencePostIds, execution.Id).ToJson(),
                Confidence = draft.Confidence,
                CreatedAt = createdAt
            };
            db.CompetitorInsights.Add(insight);
            saved.Add(insight);
        }
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Competitor analysis for business {BusinessId}: {Insights} insights from {Posts} posts", businessId, saved.Count, posts.Count);
        return new InsightAnalysisResponse(facts.Sources, facts.PostsAnalyzed, execution.Id, saved.Select(ToResponse).ToList());
    }

    // ---- background -------------------------------------------------------------------

    public async Task<(int Scanned, int Analyzed)> RunScheduledAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var scanBefore = now.AddHours(-settings.ScanEveryHours);

        var due = await db.TrackedSources
            .Where(s => (s.StatusId == StatusIds.TrackedSource.Active || s.StatusId == StatusIds.TrackedSource.Pending)
                        && (s.LastScannedAt == null || s.LastScannedAt < scanBefore)
                        && s.Business.StatusId != StatusIds.Business.Closed)
            .OrderBy(s => s.LastScannedAt)
            .Take(settings.MaxSourcesPerRun)
            .ToListAsync(cancellationToken);

        var scanned = 0;
        foreach (var tracked in due)
        {
            var outcome = await ScanCoreAsync(tracked, cancellationToken);
            if (outcome.Error is null) scanned++;
        }

        // Refresh the insights when they are old AND something new was collected since.
        var analyzeBefore = now.AddDays(-settings.AnalyzeEveryDays);
        var candidates = await db.TrackedSources
            .Where(s => s.StatusId == StatusIds.TrackedSource.Active && s.Business.StatusId != StatusIds.Business.Closed)
            .GroupBy(s => s.BusinessId)
            .Select(g => new { BusinessId = g.Key, LastScan = g.Max(s => s.LastScannedAt) })
            .OrderByDescending(x => x.LastScan)
            .Take(settings.MaxSourcesPerRun)
            .ToListAsync(cancellationToken);

        var analyzed = 0;
        foreach (var candidate in candidates)
        {
            var latest = await LatestAnalysisAsync(candidate.BusinessId, cancellationToken);
            if (latest is not null && (latest > analyzeBefore || candidate.LastScan <= latest)) continue;
            try
            {
                await AnalyzeCoreAsync(candidate.BusinessId, cancellationToken);
                analyzed++;
            }
            catch (AppException ex)
            {
                logger.LogInformation("Competitor analysis skipped for business {BusinessId}: {Reason}", candidate.BusinessId, ex.Message);
            }
        }

        return (scanned, analyzed);
    }

    // ---- scanning ---------------------------------------------------------------------

    private sealed record ScanOutcome(int Fetched, int Added, int Updated, string? Error, bool SourceProblem);

    private async Task<ScanOutcome> ScanCoreAsync(TrackedSource tracked, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        CompetitorSnapshot snapshot;
        try
        {
            snapshot = await source.FetchAsync(
                new CompetitorTarget(tracked.PlatformCode, tracked.Handle ?? string.Empty, tracked.NormalizedUrl),
                options.Value.MaxPostsPerScan, cancellationToken);
        }
        catch (CompetitorSourceException ex)
        {
            logger.LogWarning("Scan of tracked source {SourceId} failed: {Code} {Message}", tracked.Id, ex.Code, ex.Message);
            if (ex.SourceProblem)
            {
                tracked.StatusId = StatusIds.TrackedSource.Failed;
                tracked.LastScannedAt = now;
                await db.SaveChangesAsync(cancellationToken);
            }
            return new ScanOutcome(0, 0, 0, ex.Message, ex.SourceProblem);
        }

        var incoming = snapshot.Posts.DistinctBy(p => p.ExternalPostId).ToList();
        var ids = incoming.Select(p => Truncate(p.ExternalPostId, 200)!).ToList();
        var existing = await db.SourcePosts
            .Where(p => p.TrackedSourceId == tracked.Id && p.ExternalPostId != null && ids.Contains(p.ExternalPostId))
            .ToDictionaryAsync(p => p.ExternalPostId!, cancellationToken);

        int added = 0, updated = 0;
        foreach (var post in incoming)
        {
            var externalId = Truncate(post.ExternalPostId, 200)!;
            if (!existing.TryGetValue(externalId, out var row))
            {
                row = new SourcePost
                {
                    TrackedSourceId = tracked.Id,
                    ExternalPostId = externalId,
                    Format = post.Format,
                    HookType = PostText.DetectHook(post.Caption),
                    HashtagsJson = JsonSerializer.Serialize(PostText.Hashtags(post.Caption))
                };
                db.SourcePosts.Add(row);
                added++;
            }
            else
                updated++;

            // Counts grow for days after posting, so every scan refreshes them.
            row.PostUrl = Truncate(post.PostUrl, 2048);
            row.PostedAt = post.PostedAt;
            row.CaptionText = post.Caption;
            row.LikesCount = post.Likes;
            row.CommentsCount = post.Comments;
            row.ViewsCount = post.Views;
            row.EngagementRate = EngagementRate(post, snapshot.Followers);
            row.CapturedAt = now;
        }

        if (string.IsNullOrWhiteSpace(tracked.DisplayName))
            tracked.DisplayName = Truncate(snapshot.DisplayName, 200);
        tracked.StatusId = StatusIds.TrackedSource.Active;
        tracked.LastScannedAt = now;

        var tenantId = await db.Businesses.Where(b => b.Id == tracked.BusinessId).Select(b => b.TenantId).FirstAsync(cancellationToken);
        db.UsageEvents.Add(new UsageEvent
        {
            TenantId = tenantId,
            BusinessId = tracked.BusinessId,
            EventType = UsageEventType.CompetitorScan,
            Units = incoming.Count,
            EstimatedCostUsd = 0,
            IdempotencyKey = $"scan:{tracked.Id:N}:{now.ToUnixTimeMilliseconds()}",
            OccurredAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return new ScanOutcome(incoming.Count, added, updated, null, false);
    }

    // (likes + comments) / followers, kept inside the column's precision (9,6).
    internal static decimal? EngagementRate(CompetitorPost post, long? followers)
    {
        if (followers is not > 0 || (post.Likes is null && post.Comments is null)) return null;
        var rate = ((post.Likes ?? 0) + (post.Comments ?? 0)) / (decimal)followers.Value;
        return Math.Round(Math.Min(rate, 999m), 6);
    }

    // ---- helpers ----------------------------------------------------------------------

    private Task<DateTimeOffset?> LatestAnalysisAsync(Guid businessId, CancellationToken cancellationToken) =>
        db.CompetitorInsights.Where(i => i.BusinessId == businessId).MaxAsync(i => (DateTimeOffset?)i.CreatedAt, cancellationToken);

    private async Task<TrackedSource> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.TrackedSources.FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Tracked source not found.");

    private async Task<TrackedSourceResponse> LoadResponseAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var row = await Project(db.TrackedSources.AsNoTracking().Where(s => s.Id == id && s.BusinessId == businessId))
                      .FirstOrDefaultAsync(cancellationToken)
                  ?? throw new NotFoundException("Tracked source not found.");
        return ToResponse(row);
    }

    private static IQueryable<Row> Project(IQueryable<TrackedSource> sources) => sources.Select(s => new Row(
        s.Id, s.Url, s.PlatformCode, s.Handle, s.DisplayName, s.Kind, s.Notes, s.StatusId, s.Posts.Count, s.LastScannedAt, s.CreatedAt));

    private TrackedSourceResponse ToResponse(Row r) => new(
        r.Id, r.Url, r.Platform, r.Handle, r.DisplayName, r.Kind, r.Notes, statuses.Describe(r.StatusId), r.Posts, r.LastScannedAt, r.CreatedAt);

    private static CompetitorInsightResponse ToResponse(CompetitorInsight i)
    {
        var details = CompetitorInsightDetails.Parse(i.DetailsJson);
        return new CompetitorInsightResponse(i.Id, i.TrackedSourceId, i.InsightType, i.Summary, details?.ApplyToUs,
            i.Confidence, details?.EvidencePostIds ?? [], i.CreatedAt);
    }

    private static IReadOnlyList<string> Hashtags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];

    private sealed record Row(
        Guid Id, string Url, string Platform, string? Handle, string? DisplayName, TrackedSourceKind Kind, string? Notes,
        int StatusId, int Posts, DateTimeOffset? LastScannedAt, DateTimeOffset CreatedAt);
}
