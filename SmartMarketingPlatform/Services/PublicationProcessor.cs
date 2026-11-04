using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Common.Secrets;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed class PublicationProcessor(
    MarketingDbContext db,
    ISocialPublisher publisher,
    IMetricsProvider metricsProvider,
    ISecretStore secrets,
    IOptions<PublishingOptions> options,
    TimeProvider clock,
    ILogger<PublicationProcessor> logger) : IPublicationProcessor
{
    private PublishingOptions Settings => options.Value;

    public async Task<int> PublishDueJobsAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var dueIds = await db.PublicationJobs
            .Where(j => (j.StatusId == StatusIds.PublicationJob.Queued || j.StatusId == StatusIds.PublicationJob.Retry) && j.ScheduledAt <= now)
            .OrderBy(j => j.ScheduledAt)
            .Select(j => j.Id)
            .Take(Settings.BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var id in dueIds)
        {
            // Atomic claim: one UPDATE that only succeeds while the job is still waiting.
            // If two workers (two servers) see the same job, only one gets "1 row affected".
            var claimed = await db.PublicationJobs
                .Where(j => j.Id == id && (j.StatusId == StatusIds.PublicationJob.Queued || j.StatusId == StatusIds.PublicationJob.Retry))
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.StatusId, StatusIds.PublicationJob.Running), cancellationToken);
            if (claimed == 0) continue;

            if (await ProcessAsync(id, cancellationToken)) published++;
            db.ChangeTracker.Clear();   // each job starts from fresh data
        }
        return published;
    }

    // A job left Running (the process died while publishing) is put back for another attempt.
    public Task<int> RecoverStuckJobsAsync(CancellationToken cancellationToken)
    {
        var limit = clock.GetUtcNow().AddMinutes(-Settings.StuckAfterMinutes);
        return db.PublicationJobs
            .Where(j => j.StatusId == StatusIds.PublicationJob.Running && j.ScheduledAt < limit && j.Publication == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.StatusId, StatusIds.PublicationJob.Retry)
                .SetProperty(j => j.LastError, "Recovered after the worker stopped while publishing."), cancellationToken);
    }

    private async Task<bool> ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.PublicationJobs
            .Include(j => j.SocialAccount)
            .Include(j => j.ContentItem).ThenInclude(i => i.Campaign).ThenInclude(c => c.Business)
            .Include(j => j.Revision).ThenInclude(r => r.Assets).ThenInclude(a => a.Asset)
            .AsSplitQuery()
            .FirstAsync(j => j.Id == jobId, cancellationToken);
        var business = job.ContentItem.Campaign.Business;
        var now = clock.GetUtcNow();

        // Idempotency: if a publication already exists, the post went out; never publish twice.
        if (await db.Publications.AnyAsync(p => p.PublicationJobId == jobId, cancellationToken))
        {
            MarkSucceeded(job);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        // Permanent problems: no retry, the owner has to act.
        var hasSubscription = await db.Subscriptions.AnyAsync(s => s.TenantId == business.TenantId
            && s.StatusId == StatusIds.Subscription.Active && s.StartsAt <= now && s.EndsAt > now, cancellationToken);
        if (!hasSubscription)
            return await FailAsync(job, "The tenant has no active subscription.", cancellationToken);
        if (job.SocialAccount.StatusId != StatusIds.SocialAccount.Connected || job.SocialAccount.CredentialRef is null)
            return await FailAsync(job, "The social account is not connected.", cancellationToken);
        var token = await secrets.GetAsync(job.SocialAccount.CredentialRef, cancellationToken);
        if (token is null)
            return await FailAsync(job, "The social account credential is missing.", cancellationToken);

        job.AttemptCount++;
        try
        {
            var result = await publisher.PublishAsync(new PublishRequest(
                job.SocialAccount.PlatformCode,
                job.SocialAccount.ExternalAccountId,
                token,
                job.Revision.Body,
                job.Revision.Assets.OrderBy(a => a.Position).Select(a => new PublishMedia(a.Asset.ObjectKey, a.Asset.MimeType)).ToList(),
                job.IdempotencyKey), cancellationToken);

            db.Publications.Add(new Publication
            {
                PublicationJobId = job.Id,
                ExternalPostId = result.ExternalPostId,
                ExternalUrl = result.ExternalUrl,
                PublishedAt = clock.GetUtcNow()
            });
            MarkSucceeded(job);
            db.UsageEvents.Add(new UsageEvent
            {
                TenantId = business.TenantId,
                BusinessId = business.Id,
                EventType = UsageEventType.Publish,
                Units = 1,
                EstimatedCostUsd = 0,
                IdempotencyKey = $"publish:{job.Id:N}",
                OccurredAt = clock.GetUtcNow()
            });
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Published job {JobId} to {Platform}: {Url}", job.Id, job.SocialAccount.PlatformCode, result.ExternalUrl);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var transient = ex is not PublishException p || p.IsTransient;
            job.LastError = Truncate(ex.Message, 2000);

            if (transient && job.AttemptCount < Settings.MaxAttempts)
            {
                // Exponential backoff: base, 2x base, 4x base ...
                var delay = TimeSpan.FromSeconds(Settings.RetryBaseSeconds * Math.Pow(2, job.AttemptCount - 1));
                job.StatusId = StatusIds.PublicationJob.Retry;
                job.ScheduledAt = clock.GetUtcNow().Add(delay);
                await db.SaveChangesAsync(CancellationToken.None);
                logger.LogWarning("Job {JobId} attempt {Attempt} failed, retry in {Delay}: {Error}", job.Id, job.AttemptCount, delay, ex.Message);
                return false;
            }

            return await FailAsync(job, job.LastError, CancellationToken.None);
        }
    }

    private static void MarkSucceeded(PublicationJob job)
    {
        job.StatusId = StatusIds.PublicationJob.Succeeded;
        job.LastError = null;
        job.ContentItem.StatusId = StatusIds.ContentItem.Published;
    }

    // Failed for good: the content goes back to Approved so the owner can fix the cause and reschedule.
    private async Task<bool> FailAsync(PublicationJob job, string error, CancellationToken cancellationToken)
    {
        job.StatusId = StatusIds.PublicationJob.Failed;
        job.LastError = error;
        job.ContentItem.StatusId = StatusIds.ContentItem.Approved;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Job {JobId} failed: {Error}", job.Id, error);
        return false;
    }

    public async Task<int> CollectMetricsAsync(Guid? onlyPublicationId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var since = now.AddDays(-Settings.MetricsWindowDays);

        // One snapshot per interval: ObservedAt is rounded down, so a repeated run in the same
        // interval finds the snapshot already there (and the unique index backs this up).
        var observedAt = onlyPublicationId is null
            ? new DateTimeOffset(now.UtcTicks - now.UtcTicks % TimeSpan.FromSeconds(Settings.MetricsEverySeconds).Ticks, TimeSpan.Zero)
            : now;

        var targets = await db.Publications.AsNoTracking()
            .Where(p => onlyPublicationId == null ? p.PublishedAt >= since : p.Id == onlyPublicationId)
            .Where(p => p.ExternalPostId != null)
            .Select(p => new { p.Id, p.ExternalPostId, p.PublishedAt, p.PublicationJob.SocialAccount.PlatformCode, p.PublicationJob.Revision.Format })
            .ToListAsync(cancellationToken);

        var collected = 0;
        foreach (var t in targets)
        {
            if (await db.MetricObservations.AnyAsync(m => m.PublicationId == t.Id && m.ObservedAt == observedAt, cancellationToken))
                continue;

            var values = await metricsProvider.GetMetricsAsync(
                new MetricsRequest(t.PlatformCode, t.ExternalPostId!, t.PublishedAt, now, t.Format), cancellationToken);
            var raw = JsonSerializer.Serialize(values);
            foreach (var (code, value) in values)
                db.MetricObservations.Add(new MetricObservation
                {
                    PublicationId = t.Id,
                    MetricCode = code,
                    MetricValue = value,
                    ObservedAt = observedAt,
                    Source = MetricSource.PlatformApi,
                    RawPayloadJson = raw
                });
            collected++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return collected;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
