namespace SmartMarketingPlatform.Common.Publishing;

public interface ICommentGateway
{
    Task<IReadOnlyList<IncomingComment>> GetCommentsAsync(CommentTarget target, CancellationToken cancellationToken);

    Task ReplyAsync(CommentTarget target, string parentCommentId, string body, CancellationToken cancellationToken);
}

public sealed record CommentTarget(string PlatformCode, string ExternalPostId, string ExternalAccountId);

public sealed record IncomingComment(
    string ExternalCommentId,
    string? ParentExternalId,
    string? AuthorExternalId,
    string? AuthorName,
    string? AuthorUsername,
    string Body,
    DateTimeOffset PostedAt);

public sealed class CommentGatewayException(string message) : Exception(message);
