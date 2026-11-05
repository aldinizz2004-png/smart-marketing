using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// The recent messages of a WhatsApp conversation, oldest first. FromCustomer = false: written by the business.
public sealed record ChatThread(string? CustomerName, IReadOnlyList<ChatLine> Lines);

public sealed record ChatLine(bool FromCustomer, string Text, string LocalTime);

// NeedsOwner: a person must take over (an order to confirm, a complaint, a question the facts cannot answer).
// Reply is then a suggestion for the owner, never sent alone.
public sealed record CustomerReplyDraft(
    ConversationIntent Intent,
    string? Reply,
    bool NeedsOwner,
    string? HandoffReason,
    decimal Confidence);
