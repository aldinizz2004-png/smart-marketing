using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed record OutgoingMessage(string? Text, string? TemplateName, string? LanguageCode, MessageAuthor Author, Guid? UserId, Guid? AiExecutionId);

public interface IConversationMessenger
{
    // The conversation must be loaded with its SocialAccount. Saves the message and the conversation.
    Task<ConversationMessage> SendAsync(Conversation conversation, OutgoingMessage message, CancellationToken cancellationToken);

    bool CanReplyFreely(Conversation conversation);
}
