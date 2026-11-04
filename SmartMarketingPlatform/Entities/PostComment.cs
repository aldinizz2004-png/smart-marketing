using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A comment written by a customer under one of our publications.
public class PostComment
{
    public Guid Id { get; set; }
    public Guid PublicationId { get; set; }
    public string ExternalCommentId { get; set; } = string.Empty;
    public string? ParentExternalId { get; set; }               // set when it answers another comment
    public string? AuthorExternalId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorUsername { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset PostedAt { get; set; }

    public CommentCategory? Category { get; set; }
    public bool? NeedsReply { get; set; }
    public string? SuggestedReply { get; set; }
    public Guid? AnalyzedByExecutionId { get; set; }

    public string? ReplyBody { get; set; }
    public DateTimeOffset? RepliedAt { get; set; }
    public Guid? RepliedByUserId { get; set; }

    public int StatusId { get; set; }                           // Statuses (Category = "PostComment")
    public DateTimeOffset CreatedAt { get; set; }

    public Publication Publication { get; set; } = null!;
    public AiExecution? AnalyzedByExecution { get; set; }
    public AppUser? RepliedByUser { get; set; }
    public Status Status { get; set; } = null!;
}
