using SmartMarketingPlatform.Common.Integrations.WhatsApp;

namespace SmartMarketingPlatform.Services;

public interface IWhatsAppInboxService
{
    Task<(int Messages, int Statuses)> HandleWebhookAsync(WhatsAppWebhookEvent webhookEvent, CancellationToken cancellationToken);

    // Background: answer (or prepare an answer for) conversations with unread customer messages.
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);

    // allowAutoSend = false: only refresh the suggestion (the owner asked for one).
    Task RespondAsync(Guid conversationId, bool allowAutoSend, CancellationToken cancellationToken);
}
