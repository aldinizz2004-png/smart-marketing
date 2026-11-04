using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Comments;

// GET .../comments?status=Analyzed&category=Complaint&needsReply=true&platform=instagram
public sealed class CommentListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    public CommentCategory? Category { get; init; }

    public bool? NeedsReply { get; init; }

    [MaxLength(30)]
    public string? Platform { get; init; }
}

// Text is optional: without it the AI suggestion is sent.
public sealed class ReplyToCommentRequest
{
    [MaxLength(1000)]
    public string? Text { get; init; }
}

public sealed record CommentResponse(
    Guid Id,
    Guid PublicationId,
    Guid ContentItemId,
    string Platform,
    string? PostUrl,
    string? AuthorName,
    string? AuthorUsername,
    string Body,
    DateTimeOffset PostedAt,
    bool IsReply,
    CommentCategory? Category,
    bool? NeedsReply,
    string? SuggestedReply,
    string? ReplyBody,
    DateTimeOffset? RepliedAt,
    StatusInfo Status);

public sealed record CommentSyncResponse(int Publications, int Fetched, int Added);

public sealed record CommentAnalysisResponse(int Analyzed, int NeedReply, Guid? ExecutionId);
