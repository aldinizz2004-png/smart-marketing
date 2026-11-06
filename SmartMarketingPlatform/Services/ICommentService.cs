using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.DTOs.Comments;

namespace SmartMarketingPlatform.Services;

public interface ICommentService
{
    Task<PagedResult<CommentResponse>> GetAllAsync(Guid businessId, CommentListQuery query, CancellationToken cancellationToken);
    Task<CommentResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);

    Task<CommentSyncResponse> SyncAsync(Guid businessId, CancellationToken cancellationToken);
    Task<CommentAnalysisResponse> AnalyzeAsync(Guid businessId, CancellationToken cancellationToken);

    // Background: analyzes new comments of businesses with an active subscription and emails the owners about complaints.
    Task<PendingCommentsRun> AnalyzePendingAsync(CancellationToken cancellationToken);

    Task<CommentResponse> ReplyAsync(Guid businessId, Guid id, ReplyToCommentRequest request, CancellationToken cancellationToken);
    Task<CommentResponse> IgnoreAsync(Guid businessId, Guid id, CancellationToken cancellationToken);

    // Used by sync and by the webhooks. Returns false when the comment is already known or is our own reply.
    Task<bool> IngestAsync(Guid publicationId, IncomingComment comment, CancellationToken cancellationToken);
}

public sealed record PendingCommentsRun(int Businesses, int Comments, int Complaints);
