using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageDirection Direction { get; set; }
    public MessageAuthor Author { get; set; }
    public string? ExternalMessageId { get; set; }              // WhatsApp "wamid..."
    public string MessageType { get; set; } = "text";           // text, image, template ...
    public string Body { get; set; } = string.Empty;

    // Inbound: received. Outbound: sent -> delivered -> read, or failed.
    public string DeliveryStatus { get; set; } = string.Empty;
    public string? Error { get; set; }

    public Guid? SentByUserId { get; set; }
    public Guid? AiExecutionId { get; set; }

    public DateTimeOffset SentAt { get; set; }                  // the time on WhatsApp
    public DateTimeOffset CreatedAt { get; set; }               // when we stored it
    public DateTimeOffset? ProcessedAt { get; set; }            // inbound: when the AI looked at it

    public Conversation Conversation { get; set; } = null!;
    public AppUser? SentByUser { get; set; }
    public AiExecution? AiExecution { get; set; }
}
