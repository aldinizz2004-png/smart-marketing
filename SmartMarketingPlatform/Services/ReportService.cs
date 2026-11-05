using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Notifications;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Reports;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Reports;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed class ReportService(
    MarketingDbContext db,
    IBusinessAccess access,
    IPerformanceService performance,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    IEmailSender email,
    IOptions<ReportOptions> options,
    TimeProvider clock,
    ILogger<ReportService> logger) : IReportService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly int[] OpenCommentStatuses = [StatusIds.PostComment.New, StatusIds.PostComment.Analyzed];
    private static readonly int[] WaitingJobStatuses = [StatusIds.PublicationJob.Queued, StatusIds.PublicationJob.Retry];

    public async Task<PagedResult<ReportSummaryResponse>> GetAllAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return await db.OwnerReports.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Select(r => new ReportSummaryResponse(r.Id, r.PeriodStart, r.PeriodEnd, r.Headline, r.IsScheduled, r.SentAt, r.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ReportResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken));
    }

    public async Task<string> GetHtmlAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var business = await access.GetAsync(businessId, cancellationToken);
        var report = await LoadAsync(businessId, id, cancellationToken);
        return await RenderAsync(business, report, cancellationToken);
    }

    public async Task<ReportResponse> GenerateAsync(Guid businessId, GenerateReportRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var to = clock.GetUtcNow();
        var report = await CreateAsync(business, to.AddDays(-request.Days), to, isScheduled: false, cancellationToken);

        if (request.Send)
            await DeliverAsync(business, report, cancellationToken);
        return ToResponse(report);
    }

    public async Task<ReportResponse> SendAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var report = await LoadAsync(businessId, id, cancellationToken);
        await DeliverAsync(business, report, cancellationToken);
        return ToResponse(report);
    }

    public async Task<int> GenerateDueReportsAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();

        var candidates = await db.Businesses.AsNoTracking()
            .Where(b => (b.StatusId == StatusIds.Business.Active || b.StatusId == StatusIds.Business.Draft)
                        && db.Subscriptions.Any(s => s.TenantId == b.TenantId && s.StatusId == StatusIds.Subscription.Active && s.StartsAt <= now && s.EndsAt > now)
                        && db.SocialAccounts.Any(a => a.BusinessId == b.Id && a.StatusId == StatusIds.SocialAccount.Connected))
            .ToListAsync(cancellationToken);

        var made = 0;
        foreach (var business in candidates)
        {
            if (made >= settings.MaxBusinessesPerRun) break;

            var tz = TimeZoneInfo.FindSystemTimeZoneById(business.TimeZoneId);
            var boundary = ReportSchedule.LatestBoundary(now, tz, settings.WeeklyDay, settings.WeeklyHour);
            if (boundary <= business.CreatedAt) continue;
            if (await db.OwnerReports.AnyAsync(r => r.BusinessId == business.Id && r.IsScheduled && r.PeriodEnd == boundary, cancellationToken))
                continue;

            try
            {
                var report = await CreateAsync(business, boundary.AddDays(-7), boundary, isScheduled: true, cancellationToken);
                await DeliverAsync(business, report, cancellationToken);
                made++;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();   // another server made this report at the same moment
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                db.ChangeTracker.Clear();
                logger.LogWarning(ex, "Weekly report for business {BusinessId} failed; it will be tried again on the next run.", business.Id);
            }
        }
        return made;
    }

    // ---- building a report -------------------------------------------------------------

    private async Task<OwnerReport> CreateAsync(Business business, DateTimeOffset from, DateTimeOffset to, bool isScheduled, CancellationToken cancellationToken)
    {
        var facts = await BuildFactsAsync(business, from, to, cancellationToken);
        var context = await contextBuilder.BuildAsync(business.Id, null, cancellationToken);

        var (draft, execution) = await aiRunner.RunAsync(
            business.Id, AiTaskType.OwnerReport, UsageEventType.OwnerReport, facts,
            ct => ai.WriteOwnerReportAsync(context, facts, ct), cancellationToken);

        var report = new OwnerReport
        {
            BusinessId = business.Id,
            PeriodStart = from,
            PeriodEnd = to,
            FactsJson = JsonSerializer.Serialize(facts, Json),
            Headline = Truncate(draft.Headline, 300),
            Summary = Truncate(draft.Summary, 4000),
            WinsJson = JsonSerializer.Serialize(draft.Wins, Json),
            ConcernsJson = JsonSerializer.Serialize(draft.Concerns, Json),
            ActionsJson = JsonSerializer.Serialize(draft.NextActions, Json),
            GeneratedByExecutionId = execution.Id,
            IsScheduled = isScheduled
        };
        db.OwnerReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        return report;
    }

    private async Task<OwnerReportFacts> BuildFactsAsync(Business business, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(business.TimeZoneId);
        var now = clock.GetUtcNow();
        var current = await performance.ComputeAsync(business.Id, from, to, cancellationToken);
        var previous = await performance.ComputeAsync(business.Id, from - (to - from), from, cancellationToken);

        ReportTopPost? top = null;
        if (current.TopPosts.FirstOrDefault() is { } best)
        {
            var excerpt = await db.ContentRevisions.AsNoTracking()
                .Where(r => r.ContentItemId == best.ContentItemId)
                .OrderByDescending(r => r.RevisionNo)
                .Select(r => r.Body)
                .FirstOrDefaultAsync(cancellationToken);
            top = new ReportTopPost(best.PlatformCode, best.Format.ToString(), best.HookType?.ToString(), best.Reach, best.EngagementRate,
                best.ExternalUrl, excerpt is null ? null : Truncate(excerpt, 140));
        }

        var comments = db.PostComments.AsNoTracking().Where(c => c.Publication.PublicationJob.ContentItem.Campaign.BusinessId == business.Id);
        var byCategory = await comments
            .Where(c => c.PostedAt >= from && c.PostedAt < to)
            .GroupBy(c => c.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var openComplaints = await comments.CountAsync(c => c.Category == CommentCategory.Complaint && OpenCommentStatuses.Contains(c.StatusId), cancellationToken);
        var openQuestions = await comments.CountAsync(c => c.Category == CommentCategory.Question && OpenCommentStatuses.Contains(c.StatusId), cancellationToken);

        var items = db.ContentItems.AsNoTracking().Where(i => i.Campaign.BusinessId == business.Id);
        var awaitingReview = await items.CountAsync(i => i.StatusId == StatusIds.ContentItem.InReview, cancellationToken);
        var approved = await items.CountAsync(i => i.StatusId == StatusIds.ContentItem.Approved, cancellationToken);
        var scheduled = await db.PublicationJobs.CountAsync(j => j.ContentItem.Campaign.BusinessId == business.Id
            && WaitingJobStatuses.Contains(j.StatusId) && j.ScheduledAt >= now && j.ScheduledAt < now.AddDays(7), cancellationToken);
        var offersEnding = await db.Offers.AsNoTracking()
            .Where(o => o.BusinessId == business.Id && o.StatusId == StatusIds.Offer.Active && o.EndsAt > now && o.EndsAt <= now.AddDays(7))
            .OrderBy(o => o.EndsAt).Select(o => o.Title).Take(3)
            .ToListAsync(cancellationToken);

        static ReportGroup? Best(IReadOnlyList<DTOs.Publishing.PerformanceGroup> groups) =>
            groups.FirstOrDefault() is { } g ? new ReportGroup(g.Key, g.Posts, g.EngagementRate) : null;
        string Local(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, tz).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new OwnerReportFacts(
            Local(from), Local(to),
            current.Posts, previous.Posts,
            current.Totals.Reach, previous.Totals.Reach,
            current.Totals.EngagementRate, previous.Totals.EngagementRate,
            Best(current.ByPlatform), Best(current.ByFormat), Best(current.ByHook), top,
            byCategory.Sum(c => c.Count),
            byCategory.ToDictionary(c => c.Key?.ToString() ?? "NotAnalyzed", c => c.Count),
            openComplaints, openQuestions,
            awaitingReview, approved, scheduled,
            offersEnding);
    }

    // ---- delivery ----------------------------------------------------------------------

    private async Task DeliverAsync(Business business, OwnerReport report, CancellationToken cancellationToken)
    {
        var recipients = await db.TenantMemberships.AsNoTracking()
            .Where(m => m.TenantId == business.TenantId && m.StatusId == StatusIds.Membership.Active
                        && (m.Role == MembershipRole.Owner || m.Role == MembershipRole.Admin))
            .Select(m => m.User.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            throw new ConflictException("Nobody can receive this report: the tenant has no active owner or admin.");

        var html = await RenderAsync(business, report, cancellationToken);
        var arabic = await IsArabicAsync(business.Id, cancellationToken);
        var subject = arabic ? $"تقرير {business.Name}: {report.Headline}" : $"{business.Name} report: {report.Headline}";

        try
        {
            await email.SendAsync(new EmailMessage(recipients, Truncate(subject, 200), html), cancellationToken);
        }
        catch (EmailException ex)
        {
            throw new BadGatewayException(ex.Message);
        }

        report.SentAt = clock.GetUtcNow();
        report.SentTo = Truncate(string.Join(", ", recipients), 1000);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> RenderAsync(Business business, OwnerReport report, CancellationToken cancellationToken)
    {
        var response = ToResponse(report);
        var draft = new OwnerReportDraft(response.Headline, response.Summary, response.Wins, response.Concerns, response.NextActions);
        return ReportEmailRenderer.Render(business.Name, draft, response.Facts, await IsArabicAsync(business.Id, cancellationToken));
    }

    private async Task<bool> IsArabicAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var dialect = await db.BrandProfiles.AsNoTracking().Where(b => b.BusinessId == businessId).Select(b => b.Dialect).FirstOrDefaultAsync(cancellationToken);
        return dialect is null || dialect.Contains("Arabic", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<OwnerReport> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.OwnerReports.FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Report not found.");

    private static ReportResponse ToResponse(OwnerReport r) => new(
        r.Id, r.BusinessId, r.PeriodStart, r.PeriodEnd, r.Headline, r.Summary,
        JsonSerializer.Deserialize<List<string>>(r.WinsJson, Json) ?? [],
        JsonSerializer.Deserialize<List<string>>(r.ConcernsJson, Json) ?? [],
        JsonSerializer.Deserialize<List<string>>(r.ActionsJson, Json) ?? [],
        JsonSerializer.Deserialize<OwnerReportFacts>(r.FactsJson, Json)!,
        r.GeneratedByExecutionId, r.IsScheduled, r.SentAt, r.SentTo, r.CreatedAt);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
