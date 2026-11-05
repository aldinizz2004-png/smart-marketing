using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// One customer talking to the business on WhatsApp. Open = the AI handles it (suggests, or replies
// when the owner allows it); WaitingForOwner = a person must answer (order, complaint, unknown question).
public class Conversation
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SocialAccountId { get; set; }                   // our WhatsApp number
    public string CustomerPhone { get; set; } = string.Empty;   // WhatsApp id (international number, digits only)
    public string? CustomerName { get; set; }
    public int StatusId { get; set; }                           // Statuses (Category = "Conversation")

    public ConversationIntent? LastIntent { get; set; }
    public string? HandoffReason { get; set; }
    public string? SuggestedReply { get; set; }
    public Guid? SuggestedByExecutionId { get; set; }

    // WhatsApp allows free text only within 24 hours of the customer's last message.
    public DateTimeOffset? LastCustomerMessageAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public SocialAccount SocialAccount { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public AiExecution? SuggestedByExecution { get; set; }
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
