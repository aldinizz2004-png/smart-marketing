using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Brand;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Comments;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Nothing is posted under a customer's comment without a person pressing "reply":
// the AI only classifies and suggests.
public sealed class CommentService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    ICommentGateway gateway,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    IBrandGuard brandGuard,
    StatusCatalog statuses,
    TimeProvider clock,
    ILogger<CommentService> logger) : ICommentService
{
    private const int SyncWindowDays = 30;
    private const int MaxPublicationsPerSync = 50;
    private const int MaxCommentsPerAnalysis = 25;

    public async Task<PagedResult<CommentResponse>> GetAllAsync(Guid businessId, CommentListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var comments = OfBusiness(businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("PostComment", query.Status);
            comments = comments.Where(c => c.StatusId == statusId);
        }
        if (query.Category is { } category)
            comments = comments.Where(c => c.Category == category);
        if (query.NeedsReply is { } needsReply)
            comments = comments.Where(c => c.NeedsReply == needsReply);
        if (!string.IsNullOrWhiteSpace(query.Platform))
            comments = comments.Where(c => c.Publication.PublicationJob.SocialAccount.PlatformCode == query.Platform.ToLower());

        var page = await Project(comments.OrderByDescending(c => c.PostedAt).ThenBy(c => c.Id))
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<CommentResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    public async Task<CommentSyncResponse> SyncAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var since = clock.GetUtcNow().AddDays(-SyncWindowDays);

        var publications = await db.Publications.AsNoTracking()
            .Where(p => p.PublicationJob.ContentItem.Campaign.BusinessId == businessId && p.PublishedAt >= since && p.ExternalPostId != null)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new
            {
                p.Id,
                Target = new CommentTarget(p.PublicationJob.SocialAccount.PlatformCode, p.ExternalPostId!, p.PublicationJob.SocialAccount.ExternalAccountId)
            })
            .Take(MaxPublicationsPerSync)
            .ToListAsync(cancellationToken);

        int fetched = 0, added = 0;
        foreach (var publication in publications)
        {
            IReadOnlyList<IncomingComment> incoming;
            try
            {
                incoming = await gateway.GetCommentsAsync(publication.Target, cancellationToken);
            }
            catch (CommentGatewayException ex)
            {
                throw new BadGatewayException($"Could not read the comments from the social media provider: {ex.Message}");
            }

            fetched += incoming.Count;
            foreach (var comment in incoming)
                if (await IngestAsync(publication.Id, comment, cancellationToken))
                    added++;
        }

        return new CommentSyncResponse(publications.Count, fetched, added);
    }

    public async Task<bool> IngestAsync(Guid publicationId, IncomingComment comment, CancellationToken cancellationToken)
    {
        if (await db.PostComments.AnyAsync(c => c.PublicationId == publicationId && c.ExternalCommentId == comment.ExternalCommentId, cancellationToken))
            return false;

        // Platforms send our own replies back as new comments; they are recognized by their text.
        var isOurReply = comment.ParentExternalId is not null && await db.PostComments.AnyAsync(c =>
            c.PublicationId == publicationId && c.ReplyBody == comment.Body, cancellationToken);
        if (isOurReply)
            return false;

        var entity = new PostComment
        {
            PublicationId = publicationId,
            ExternalCommentId = Truncate(comment.ExternalCommentId, 200)!,
            ParentExternalId = Truncate(comment.ParentExternalId, 200),
            AuthorExternalId = Truncate(comment.AuthorExternalId, 200),
            AuthorName = Truncate(comment.AuthorName, 200),
            AuthorUsername = Truncate(comment.AuthorUsername, 100),
            Body = Truncate(comment.Body, 4000)!,
            PostedAt = comment.PostedAt,
            StatusId = StatusIds.PostComment.New
        };
        db.PostComments.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<CommentAnalysisResponse> AnalyzeAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var pending = await OfBusiness(businessId)
            .Where(c => c.StatusId == StatusIds.PostComment.New)
            .OrderBy(c => c.PostedAt)
            .Take(MaxCommentsPerAnalysis)
            .Include(c => c.Publication).ThenInclude(p => p.PublicationJob).ThenInclude(j => j.SocialAccount)
            .Include(c => c.Publication).ThenInclude(p => p.PublicationJob).ThenInclude(j => j.Revision)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
            return new CommentAnalysisResponse(0, 0, null);

        var context = await contextBuilder.BuildAsync(businessId, null, cancellationToken);
        var inputs = pending.Select(c => new CommentInput(
            c.Id, c.Publication.PublicationJob.SocialAccount.PlatformCode, c.AuthorName, c.Body, c.Publication.PublicationJob.Revision.Body)).ToList();

        var (analyses, execution) = await aiRunner.RunAsync(
            businessId, AiTaskType.CommentAnalysis, UsageEventType.CommentAnalysis, new { comments = inputs },
            ct => ai.AnalyzeCommentsAsync(context, inputs, ct), cancellationToken);

        var byId = pending.ToDictionary(c => c.Id);
        foreach (var analysis in analyses)
        {
            if (!byId.TryGetValue(analysis.CommentId, out var comment)) continue;
            comment.Category = analysis.Category;
            comment.NeedsReply = analysis.NeedsReply;
            comment.SuggestedReply = Truncate(analysis.SuggestedReply, 2000);
            comment.AnalyzedByExecutionId = execution.Id;
            comment.StatusId = StatusIds.PostComment.Analyzed;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new CommentAnalysisResponse(analyses.Count, analyses.Count(a => a.NeedsReply), execution.Id);
    }

    public async Task<CommentResponse> ReplyAsync(Guid businessId, Guid id, ReplyToCommentRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var comment = await LoadAsync(businessId, id, cancellationToken);
        if (comment.StatusId == StatusIds.PostComment.Replied)
            throw new ConflictException("This comment was already answered.");

        var text = string.IsNullOrWhiteSpace(request.Text) ? comment.SuggestedReply : request.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new BadRequestException("Write a reply, or analyze the comments first to get a suggestion.");

        var banned = await brandGuard.FindProhibitedTermsAsync(businessId, text, cancellationToken);
        if (banned.Count > 0)
            throw new BadRequestException($"The reply uses prohibited words: {string.Join(", ", banned)}.");

        var account = comment.Publication.PublicationJob.SocialAccount;
        if (account.StatusId != StatusIds.SocialAccount.Connected)
            throw new ConflictException("The social account of this post is not connected. Reconnect it first.");

        try
        {
            await gateway.ReplyAsync(
                new CommentTarget(account.PlatformCode, comment.Publication.ExternalPostId!, account.ExternalAccountId),
                comment.ExternalCommentId, text, cancellationToken);
        }
        catch (CommentGatewayException ex)
        {
            throw new BadGatewayException($"The reply could not be sent: {ex.Message}");
        }

        comment.ReplyBody = text;
        comment.RepliedAt = clock.GetUtcNow();
        comment.RepliedByUserId = currentUser.UserId;
        comment.StatusId = StatusIds.PostComment.Replied;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Comment {CommentId} answered on {Platform}", comment.Id, account.PlatformCode);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    public async Task<CommentResponse> IgnoreAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var comment = await LoadAsync(businessId, id, cancellationToken);
        if (comment.StatusId == StatusIds.PostComment.Replied)
            throw new ConflictException("This comment was already answered.");

        comment.StatusId = StatusIds.PostComment.Ignored;
        await db.SaveChangesAsync(cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    private IQueryable<PostComment> OfBusiness(Guid businessId) =>
        db.PostComments.Where(c => c.Publication.PublicationJob.ContentItem.Campaign.BusinessId == businessId);

    private async Task<PostComment> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await OfBusiness(businessId)
            .Include(c => c.Publication).ThenInclude(p => p.PublicationJob).ThenInclude(j => j.SocialAccount)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Comment not found.");

    private async Task<CommentResponse> LoadResponseAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var row = await Project(OfBusiness(businessId).AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(cancellationToken)
                  ?? throw new NotFoundException("Comment not found.");
        return ToResponse(row);
    }

    private static IQueryable<Row> Project(IQueryable<PostComment> comments) => comments.Select(c => new Row(
        c.Id, c.PublicationId, c.Publication.PublicationJob.ContentItemId, c.Publication.PublicationJob.SocialAccount.PlatformCode,
        c.Publication.ExternalUrl, c.AuthorName, c.AuthorUsername, c.Body, c.PostedAt, c.ParentExternalId != null,
        c.Category, c.NeedsReply, c.SuggestedReply, c.ReplyBody, c.RepliedAt, c.StatusId));

    private CommentResponse ToResponse(Row r) => new(
        r.Id, r.PublicationId, r.ContentItemId, r.Platform, r.PostUrl, r.AuthorName, r.AuthorUsername, r.Body, r.PostedAt,
        r.IsReply, r.Category, r.NeedsReply, r.SuggestedReply, r.ReplyBody, r.RepliedAt, statuses.Describe(r.StatusId));

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];

    private sealed record Row(
        Guid Id, Guid PublicationId, Guid ContentItemId, string Platform, string? PostUrl, string? AuthorName, string? AuthorUsername,
        string Body, DateTimeOffset PostedAt, bool IsReply, CommentCategory? Category, bool? NeedsReply, string? SuggestedReply,
        string? ReplyBody, DateTimeOffset? RepliedAt, int StatusId);
}
