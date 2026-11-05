using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Brand;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Common.Secrets;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// The only place that sends a WhatsApp message, for the AI and for people alike:
// same banned-words check, same 24-hour rule, same usage record.
public sealed class ConversationMessenger(
    MarketingDbContext db,
    IWhatsAppGateway gateway,
    ISecretStore secrets,
    IBrandGuard brandGuard,
    IOptions<WhatsAppOptions> options,
    TimeProvider clock) : IConversationMessenger
{
    public bool CanReplyFreely(Conversation conversation) =>
        conversation.LastCustomerMessageAt is { } last && clock.GetUtcNow() - last < TimeSpan.FromHours(options.Value.ReplyWindowHours);

    public async Task<ConversationMessage> SendAsync(Conversation conversation, OutgoingMessage message, CancellationToken cancellationToken)
    {
        var account = conversation.SocialAccount;
        if (account.StatusId != StatusIds.SocialAccount.Connected || account.CredentialRef is null)
            throw new ConflictException("The WhatsApp number of this business is not connected. Reconnect it first.");
        var token = await secrets.GetAsync(account.CredentialRef, cancellationToken)
                    ?? throw new ConflictException("The WhatsApp token is missing. Update the account credentials.");

        var isTemplate = !string.IsNullOrWhiteSpace(message.TemplateName);
        var text = message.Text?.Trim();
        if (!isTemplate)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new BadRequestException("The message is empty.");
            if (!CanReplyFreely(conversation))
                throw new ConflictException($"More than {options.Value.ReplyWindowHours} hours passed since the customer's last message. Send an approved template (templateName).");
            var banned = await brandGuard.FindProhibitedTermsAsync(conversation.BusinessId, text, cancellationToken);
            if (banned.Count > 0)
                throw new BadRequestException($"The message uses prohibited words: {string.Join(", ", banned)}.");
        }

        var sender = new WhatsAppSender(account.ExternalAccountId, token);
        string externalId;
        try
        {
            externalId = isTemplate
                ? await gateway.SendTemplateAsync(sender, conversation.CustomerPhone, message.TemplateName!.Trim(), message.LanguageCode?.Trim() ?? "ar", cancellationToken)
                : await gateway.SendTextAsync(sender, conversation.CustomerPhone, text!, cancellationToken);
        }
        catch (WhatsAppGatewayException ex) when (ex.OutsideWindow)
        {
            throw new ConflictException(ex.Message);
        }
        catch (WhatsAppGatewayException ex)
        {
            throw new BadGatewayException($"WhatsApp did not accept the message: {ex.Message}");
        }

        var now = clock.GetUtcNow();
        var sent = new ConversationMessage
        {
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            Author = message.Author,
            ExternalMessageId = externalId,
            MessageType = isTemplate ? "template" : "text",
            Body = isTemplate ? $"[template] {message.TemplateName!.Trim()}" : text!,
            DeliveryStatus = "sent",
            SentByUserId = message.UserId,
            AiExecutionId = message.AiExecutionId,
            SentAt = now,
            CreatedAt = now
        };
        db.ConversationMessages.Add(sent);

        // Once someone answered, the old suggestion no longer fits the conversation.
        conversation.SuggestedReply = null;
        conversation.SuggestedByExecutionId = null;
        conversation.LastMessageAt = now;

        var tenantId = await db.Businesses.Where(b => b.Id == conversation.BusinessId).Select(b => b.TenantId).FirstAsync(cancellationToken);
        db.UsageEvents.Add(new UsageEvent
        {
            TenantId = tenantId,
            BusinessId = conversation.BusinessId,
            EventType = UsageEventType.WhatsAppMessage,
            Units = 1,
            EstimatedCostUsd = 0,
            IdempotencyKey = $"wa:{externalId}"[..Math.Min(100, externalId.Length + 3)],
            OccurredAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
