using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Publishing;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Turns approved content into publication jobs, inside the owner's limits:
// the publish window (business local time) and the maximum posts per day.
public sealed class PublishingService(
    MarketingDbContext db,
    IBusinessAccess access,
    StatusCatalog statuses,
    TimeProvider clock,
    ILogger<PublishingService> logger) : IPublishingService
{
    private const int SearchDays = 30;

    // Jobs that use a slot of the day (a canceled or failed job frees it).
    private static readonly int[] CountingStatuses =
    [
        StatusIds.PublicationJob.Queued, StatusIds.PublicationJob.Running,
        StatusIds.PublicationJob.Retry, StatusIds.PublicationJob.Succeeded
    ];

    private static readonly int[] Cancelable = [StatusIds.PublicationJob.Queued, StatusIds.PublicationJob.Retry];

    private sealed record OwnerLimits(TimeZoneInfo TimeZone, TimeOnly? WindowStart, TimeOnly? WindowEnd, short MaxPostsPerDay);

    public async Task<PublicationJobResponse> ScheduleAsync(Guid businessId, Guid contentItemId, ScheduleRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var item = await LoadItemAsync(businessId, contentItemId, cancellationToken);
        var account = await ResolveAccountAsync(businessId, item.PlatformCode, request.SocialAccountId, cancellationToken)
                      ?? throw new ConflictException($"Connect a {item.PlatformCode} account first (POST .../social-accounts).");

        var limits = await LoadLimitsAsync(businessId, cancellationToken);
        var now = clock.GetUtcNow();
        DateTimeOffset at;

        if (request.PublishAt is { } wanted)
        {
            // An explicit time is checked strictly: it is the owner's choice, we do not move it.
            if (wanted < now.AddMinutes(-1))
                throw new BadRequestException("publishAt is in the past.");
            at = wanted < now ? now : wanted;
            if (!InWindow(at, limits))
                throw new BadRequestException($"publishAt is outside the owner's publish window ({limits.WindowStart:HH:mm}-{limits.WindowEnd:HH:mm}, {limits.TimeZone.Id}).");
            if (await CountForDayAsync(businessId, at, limits, cancellationToken) >= limits.MaxPostsPerDay)
                throw new ConflictException($"The owner's limit of {limits.MaxPostsPerDay} posts per day is already reached for that day.");
        }
        else
        {
            var candidate = item.PlannedPublishAt is { } planned && planned > now ? planned : now;   // no time given = as soon as allowed
            at = await FindSlotAsync(businessId, candidate, limits, cancellationToken);
        }

        var job = await CreateJobAsync(item, account, at, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetJobAsync(businessId, job.Id, cancellationToken);
    }

    public async Task<bool> TryAutoScheduleAsync(Guid businessId, Guid contentItemId, CancellationToken cancellationToken)
    {
        var policy = await db.AutomationPolicies.AsNoTracking().Include(p => p.AllowedPlatforms)
            .FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);
        if (policy is not { AutomationEnabled: true }) return false;

        var item = await LoadItemAsync(businessId, contentItemId, cancellationToken);
        if (policy.AllowedPlatforms.All(p => p.PlatformCode != item.PlatformCode)) return false;

        var account = await ResolveAccountAsync(businessId, item.PlatformCode, null, cancellationToken);
        if (account is null) return false;

        try
        {
            var limits = await LoadLimitsAsync(businessId, cancellationToken);
            var now = clock.GetUtcNow();
            var candidate = item.PlannedPublishAt is { } planned && planned > now ? planned : now;   // no time given = as soon as allowed
            var at = await FindSlotAsync(businessId, candidate, limits, cancellationToken);

            await CreateJobAsync(item, account, at, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (AppException ex)
        {
            // Auto-scheduling is a convenience: if it is not possible, the item simply stays Approved.
            logger.LogInformation("Auto-scheduling of {ContentItemId} skipped: {Reason}", contentItemId, ex.Message);
            return false;
        }
    }

    public async Task<PublicationJobResponse> CancelJobAsync(Guid businessId, Guid jobId, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var job = await db.PublicationJobs.Include(j => j.ContentItem)
                      .FirstOrDefaultAsync(j => j.Id == jobId && j.ContentItem.Campaign.BusinessId == businessId, cancellationToken)
                  ?? throw new NotFoundException("Publication job not found.");

        if (!Cancelable.Contains(job.StatusId))
            throw new ConflictException($"A {statuses.CodeOf(job.StatusId)} job cannot be canceled.");

        job.StatusId = StatusIds.PublicationJob.Canceled;
        job.ContentItem.StatusId = StatusIds.ContentItem.Approved;      // back to "ready to schedule"
        await db.SaveChangesAsync(cancellationToken);
        return await GetJobAsync(businessId, jobId, cancellationToken);
    }

    // Used when the content itself is canceled while scheduled.
    public Task<int> CancelActiveJobsAsync(Guid contentItemId, CancellationToken cancellationToken) =>
        db.PublicationJobs
            .Where(j => j.ContentItemId == contentItemId && Cancelable.Contains(j.StatusId))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.StatusId, StatusIds.PublicationJob.Canceled), cancellationToken);

    public async Task<PagedResult<PublicationJobResponse>> GetJobsAsync(Guid businessId, JobListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var jobs = Jobs(businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("PublicationJob", query.Status);
            jobs = jobs.Where(j => j.StatusId == statusId);
        }

        var page = await jobs.OrderByDescending(j => j.ScheduledAt).ThenByDescending(j => j.Id).ToPagedResultAsync(query, cancellationToken);
        var latest = await LatestMetricsAsync(page.Items.Where(j => j.Publication != null).Select(j => j.Publication!.Id).ToList(), cancellationToken);
        return page.Map(j => ToResponse(j, latest));
    }

    public async Task<PublicationJobResponse> GetJobAsync(Guid businessId, Guid jobId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var job = await Jobs(businessId).FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
                  ?? throw new NotFoundException("Publication job not found.");
        var latest = await LatestMetricsAsync(job.Publication is null ? [] : [job.Publication.Id], cancellationToken);
        return ToResponse(job, latest);
    }

    // ---- scheduling rules ---------------------------------------------------------

    private async Task<PublicationJob> CreateJobAsync(ContentItem item, SocialAccount account, DateTimeOffset at, CancellationToken cancellationToken)
    {
        if (item.StatusId != StatusIds.ContentItem.Approved)
            throw new ConflictException(item.StatusId == StatusIds.ContentItem.Scheduled
                ? "This content is already scheduled. Cancel its job to reschedule."
                : "Only approved content can be scheduled.");

        // The exact revision that was approved, and the approval itself, are frozen into the job.
        var approved = await db.ContentRevisions.AsNoTracking()
            .Where(r => r.ContentItemId == item.Id)
            .OrderByDescending(r => r.RevisionNo)
            .Select(r => new
            {
                r.Id,
                r.RevisionNo,
                ReviewId = r.Reviews.Where(v => v.StatusId == StatusIds.ContentReview.Approved)
                    .OrderByDescending(v => v.DecidedAt).Select(v => (Guid?)v.Id).FirstOrDefault()
            })
            .FirstAsync(cancellationToken);
        if (approved.ReviewId is null)
            throw new ConflictException("The latest revision has no approval.");

        // Same item + revision + account => same key family. Two parallel requests compute the same
        // key and the unique index (SocialAccountId, IdempotencyKey) lets only one of them in (409).
        var previous = await db.PublicationJobs.CountAsync(j => j.ContentItemId == item.Id && j.RevisionId == approved.Id, cancellationToken);
        var job = new PublicationJob
        {
            ContentItemId = item.Id,
            RevisionId = approved.Id,
            ApprovedReviewId = approved.ReviewId.Value,
            SocialAccountId = account.Id,
            ScheduledAt = at,
            StatusId = StatusIds.PublicationJob.Queued,
            IdempotencyKey = $"item:{item.Id:N}:rev:{approved.RevisionNo}:v{previous + 1}",
            SourceRecommendationId = item.SourceRecommendationId
        };
        db.PublicationJobs.Add(job);
        item.StatusId = StatusIds.ContentItem.Scheduled;
        return job;
    }

    // First moment >= candidate that is inside the window and on a day that still has room.
    private async Task<DateTimeOffset> FindSlotAsync(Guid businessId, DateTimeOffset candidate, OwnerLimits limits, CancellationToken cancellationToken)
    {
        var at = candidate;
        for (var day = 0; day < SearchDays; day++)
        {
            at = MoveIntoWindow(at, limits);
            if (await CountForDayAsync(businessId, at, limits, cancellationToken) < limits.MaxPostsPerDay)
                return at;
            at = StartOfNextDay(at, limits);
        }
        throw new ConflictException($"No free publishing slot in the next {SearchDays} days within the owner's limits.");
    }

    private static DateTimeOffset MoveIntoWindow(DateTimeOffset at, OwnerLimits limits)
    {
        if (limits.WindowStart is not { } start || limits.WindowEnd is not { } end) return at;
        var local = TimeZoneInfo.ConvertTime(at, limits.TimeZone);
        var time = TimeOnly.FromDateTime(local.DateTime);
        if (time < start) return AtLocal(local.Date, start, limits.TimeZone);
        if (time >= end) return AtLocal(local.Date.AddDays(1), start, limits.TimeZone);
        return at;
    }

    private static DateTimeOffset StartOfNextDay(DateTimeOffset at, OwnerLimits limits)
    {
        var local = TimeZoneInfo.ConvertTime(at, limits.TimeZone);
        return AtLocal(local.Date.AddDays(1), limits.WindowStart ?? TimeOnly.MinValue, limits.TimeZone);
    }

    private static bool InWindow(DateTimeOffset at, OwnerLimits limits)
    {
        if (limits.WindowStart is not { } start || limits.WindowEnd is not { } end) return true;
        var time = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, limits.TimeZone).DateTime);
        return time >= start && time < end;
    }

    // Jobs of the whole business on the LOCAL calendar day of "at".
    private Task<int> CountForDayAsync(Guid businessId, DateTimeOffset at, OwnerLimits limits, CancellationToken cancellationToken)
    {
        var localDay = TimeZoneInfo.ConvertTime(at, limits.TimeZone).Date;
        var from = AtLocal(localDay, TimeOnly.MinValue, limits.TimeZone);
        var to = AtLocal(localDay.AddDays(1), TimeOnly.MinValue, limits.TimeZone);
        return db.PublicationJobs.CountAsync(j =>
            j.ContentItem.Campaign.BusinessId == businessId
            && CountingStatuses.Contains(j.StatusId)
            && j.ScheduledAt >= from && j.ScheduledAt < to, cancellationToken);
    }

    private static DateTimeOffset AtLocal(DateTime localDate, TimeOnly time, TimeZoneInfo tz)
    {
        var local = localDate.Date + time.ToTimeSpan();
        return new DateTimeOffset(local, tz.GetUtcOffset(local)).ToUniversalTime();
    }

    private async Task<OwnerLimits> LoadLimitsAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var timeZoneId = await db.Businesses.Where(b => b.Id == businessId).Select(b => b.TimeZoneId).FirstAsync(cancellationToken);
        var policy = await db.AutomationPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);
        return new OwnerLimits(
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId),
            policy?.PublishWindowStart,
            policy?.PublishWindowEnd,
            policy?.MaxPostsPerDay ?? 3);
    }

    // ---- loading --------------------------------------------------------------------

    private async Task<ContentItem> LoadItemAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.ContentItems.FirstOrDefaultAsync(i => i.Id == id && i.Campaign.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Content item not found.");

    private async Task<SocialAccount?> ResolveAccountAsync(Guid businessId, string platformCode, Guid? accountId, CancellationToken cancellationToken)
    {
        if (accountId is { } id)
        {
            var chosen = await db.SocialAccounts.FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken)
                         ?? throw new NotFoundException("Social account not found.");
            if (chosen.PlatformCode != platformCode)
                throw new BadRequestException($"This content is for {platformCode}; the account is on {chosen.PlatformCode}.");
            if (chosen.StatusId != StatusIds.SocialAccount.Connected || chosen.CredentialRef is null)
                throw new ConflictException("This account is not connected. Reconnect it first.");
            return chosen;
        }

        return await db.SocialAccounts
            .Where(s => s.BusinessId == businessId && s.PlatformCode == platformCode
                        && s.StatusId == StatusIds.SocialAccount.Connected && s.CredentialRef != null)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<PublicationJob> Jobs(Guid businessId) =>
        db.PublicationJobs.AsNoTracking()
            .Include(j => j.SocialAccount)
            .Include(j => j.Publication)
            .Where(j => j.ContentItem.Campaign.BusinessId == businessId);

    private async Task<Dictionary<Guid, (Dictionary<MetricCode, decimal> Values, DateTimeOffset At)>> LatestMetricsAsync(
        List<Guid> publicationIds, CancellationToken cancellationToken)
    {
        if (publicationIds.Count == 0) return [];
        var rows = await db.MetricObservations.AsNoTracking()
            .Where(m => publicationIds.Contains(m.PublicationId)
                        && m.ObservedAt == db.MetricObservations.Where(x => x.PublicationId == m.PublicationId).Max(x => x.ObservedAt))
            .ToListAsync(cancellationToken);
        return rows.GroupBy(r => r.PublicationId)
            .ToDictionary(g => g.Key, g => (g.ToDictionary(r => r.MetricCode, r => r.MetricValue), g.First().ObservedAt));
    }

    private PublicationJobResponse ToResponse(PublicationJob j, Dictionary<Guid, (Dictionary<MetricCode, decimal> Values, DateTimeOffset At)> latest)
    {
        PublicationResponse? publication = null;
        if (j.Publication is { } p)
        {
            var found = latest.TryGetValue(p.Id, out var m);
            publication = new PublicationResponse(p.Id, j.Id, j.ContentItemId, j.SocialAccount.PlatformCode, p.ExternalPostId, p.ExternalUrl,
                p.PublishedAt, found ? m.Values : new Dictionary<MetricCode, decimal>(), found ? m.At : null);
        }
        return new PublicationJobResponse(j.Id, j.ContentItemId, j.RevisionId, j.SocialAccountId, j.SocialAccount.PlatformCode,
            j.ScheduledAt, statuses.Describe(j.StatusId), j.AttemptCount, j.LastError, publication, j.CreatedAt);
    }
}
