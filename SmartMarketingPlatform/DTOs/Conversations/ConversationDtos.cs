using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Conversations;

// GET .../conversations?status=WaitingForOwner&intent=Order
public sealed class ConversationListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    public ConversationIntent? Intent { get; init; }
}

// Text = our own words. Empty body = send the AI suggestion. TemplateName = an approved template
// (the only thing WhatsApp accepts more than 24 hours after the customer's last message).
public sealed class SendMessageRequest
{
    [MaxLength(1000)]
    public string? Text { get; init; }

    [MaxLength(100)]
    public string? TemplateName { get; init; }

    [MaxLength(10)]
    public string? LanguageCode { get; init; }
}

public sealed record ConversationSummaryResponse(
    Guid Id,
    string CustomerPhone,
    string? CustomerName,
    StatusInfo Status,
    ConversationIntent? LastIntent,
    string? HandoffReason,
    bool HasSuggestion,
    string? LastMessage,
    DateTimeOffset LastMessageAt,
    bool CanReplyFreely,
    DateTimeOffset? ReplyWindowEndsAt);

public sealed record ConversationResponse(
    ConversationSummaryResponse Conversation,
    string? SuggestedReply,
    IReadOnlyList<MessageResponse> Messages);

public sealed record MessageResponse(
    Guid Id,
    MessageDirection Direction,
    MessageAuthor Author,
    string MessageType,
    string Body,
    string DeliveryStatus,
    string? Error,
    Guid? SentByUserId,
    DateTimeOffset SentAt);
