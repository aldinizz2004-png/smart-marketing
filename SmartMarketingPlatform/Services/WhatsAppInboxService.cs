using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Integrations.WhatsApp;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Common.Notifications;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Webhook in, AI decision, reply out. The webhook only stores messages (Meta expects a fast 200);
// the worker answers a few seconds later, so several short messages in a row get one answer.
public sealed class WhatsAppInboxService(
    MarketingDbContext db,
    IConversationMessenger messenger,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    IEmailSender email,
    IOptions<WhatsAppOptions> options,
    TimeProvider clock,
    ILogger<WhatsAppInboxService> logger) : IWhatsAppInboxService
{
    private const string AiUnavailable = "The AI could not answer this conversation. Please reply yourself.";

    private static readonly Dictionary<string, int> DeliveryRank = new()
    {
        ["sent"] = 1, ["delivered"] = 2, ["read"] = 3, ["failed"] = 4
    };

    // ---- webhook -------------------------------------------------------------------

    public async Task<(int Messages, int Statuses)> HandleWebhookAsync(WhatsAppWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        int stored = 0, updated = 0;
        var accounts = new Dictionary<string, (Guid Id, Guid BusinessId)?>();

        foreach (var incoming in webhookEvent.Messages)
        {
            if (!accounts.TryGetValue(incoming.PhoneNumberId, out var account))
            {
                var row = await db.SocialAccounts.AsNoTracking()
                    .Where(s => s.PlatformCode == "whatsapp" && s.ExternalAccountId == incoming.PhoneNumberId
                                && s.StatusId == StatusIds.SocialAccount.Connected)
                    .Select(s => new { s.Id, s.BusinessId })
                    .FirstOrDefaultAsync(cancellationToken);
                account = row is null ? null : (row.Id, row.BusinessId);
                accounts[incoming.PhoneNumberId] = account;
            }
            if (account is not { } known)
            {
                logger.LogWarning("WhatsApp message for unknown number {PhoneNumberId} ignored.", incoming.PhoneNumberId);
                continue;
            }

            if (await db.ConversationMessages.AnyAsync(m => m.ExternalMessageId == incoming.MessageId, cancellationToken))
                continue;

            var phone = Truncate(incoming.From, 32)!;
            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.SocialAccountId == known.Id && c.CustomerPhone == phone, cancellationToken);
            if (conversation is null)
            {
                conversation = new Conversation
                {
                    BusinessId = known.BusinessId,
                    SocialAccountId = known.Id,
                    CustomerPhone = phone,
                    StatusId = StatusIds.Conversation.Open,
                    CreatedAt = now,
                    LastMessageAt = incoming.SentAt
                };
                db.Conversations.Add(conversation);
            }
            else if (conversation.StatusId == StatusIds.Conversation.Closed)
            {
                conversation.StatusId = StatusIds.Conversation.Open;
                conversation.HandoffReason = null;
            }

            conversation.CustomerName = Truncate(incoming.ProfileName, 200) ?? conversation.CustomerName;
            if (conversation.LastCustomerMessageAt is null || incoming.SentAt > conversation.LastCustomerMessageAt)
                conversation.LastCustomerMessageAt = incoming.SentAt;
            if (incoming.SentAt > conversation.LastMessageAt)
                conversation.LastMessageAt = incoming.SentAt;

            db.ConversationMessages.Add(new ConversationMessage
            {
                Conversation = conversation,
                Direction = MessageDirection.Inbound,
                Author = MessageAuthor.Customer,
                ExternalMessageId = Truncate(incoming.MessageId, 200),
                MessageType = Truncate(incoming.Type, 30)!,
                Body = Truncate(string.IsNullOrWhiteSpace(incoming.Text) ? $"[{incoming.Type}]" : incoming.Text, 4096)!,
                DeliveryStatus = "received",
                SentAt = incoming.SentAt,
                CreatedAt = now
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                stored++;
            }
            catch (DbUpdateException ex)
            {
                // The same webhook delivered twice at the same moment: the other request stored it.
                logger.LogInformation(ex, "WhatsApp message {MessageId} was already stored.", incoming.MessageId);
                db.ChangeTracker.Clear();
            }
        }

        foreach (var status in webhookEvent.Statuses)
        {
            var message = await db.ConversationMessages
                .FirstOrDefaultAsync(m => m.ExternalMessageId == status.MessageId && m.Direction == MessageDirection.Outbound, cancellationToken);
            if (message is null) continue;

            // Webhooks can arrive out of order: "delivered" never overwrites "read".
            var newRank = DeliveryRank.GetValueOrDefault(status.Status);
            if (newRank > DeliveryRank.GetValueOrDefault(message.DeliveryStatus))
            {
                message.DeliveryStatus = Truncate(status.Status, 20)!;
                message.Error = Truncate(status.Error, 500);
                updated++;
            }
        }
        if (updated > 0)
            await db.SaveChangesAsync(cancellationToken);

        return (stored, updated);
    }

    // ---- answering -----------------------------------------------------------------

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var quietSince = clock.GetUtcNow().AddSeconds(-settings.DebounceSeconds);

        var due = await db.ConversationMessages
            .Where(m => m.Direction == MessageDirection.Inbound && m.ProcessedAt == null)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Last = g.Max(m => m.CreatedAt) })
            .Where(x => x.Last <= quietSince)
            .OrderBy(x => x.Last)
            .Take(settings.MaxConversationsPerRun)
            .Select(x => x.ConversationId)
            .ToListAsync(cancellationToken);

        var handled = 0;
        foreach (var conversationId in due)
        {
            try
            {
                await RespondAsync(conversationId, allowAutoSend: true, cancellationToken);
                handled++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not handle WhatsApp conversation {ConversationId}.", conversationId);
                db.ChangeTracker.Clear();
            }
        }
        return handled;
    }

    public async Task RespondAsync(Guid conversationId, bool allowAutoSend, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var conversation = await db.Conversations.Include(c => c.SocialAccount)
            .FirstAsync(c => c.Id == conversationId, cancellationToken);

        var history = await db.ConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt).Take(settings.HistoryMessages)
            .ToListAsync(cancellationToken);
        history.Reverse();

        var context = await contextBuilder.BuildAsync(conversation.BusinessId, null, cancellationToken);
        var tz = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZoneId);
        var thread = new ChatThread(conversation.CustomerName, history.Select(m => new ChatLine(
            m.Direction == MessageDirection.Inbound, m.Body,
            TimeZoneInfo.ConvertTime(m.SentAt, tz).ToString("ddd HH:mm", CultureInfo.InvariantCulture))).ToList());

        // Read now: whatever happens next, these messages will not be answered twice.
        var now = clock.GetUtcNow();
        await db.ConversationMessages
            .Where(m => m.ConversationId == conversationId && m.Direction == MessageDirection.Inbound && m.ProcessedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedAt, now), cancellationToken);

        CustomerReplyDraft draft;
        AiExecution execution;
        try
        {
            (draft, execution) = await aiRunner.RunAsync(
                conversation.BusinessId, AiTaskType.CustomerReply, UsageEventType.CustomerReply, new { conversationId, thread },
                ct => ai.ReplyToCustomerAsync(context, thread, ct), cancellationToken);
        }
        catch (AppException ex)
        {
            logger.LogWarning("AI reply failed for conversation {ConversationId}: {Message}", conversationId, ex.Message);
            await HandOffAsync(conversation, AiUnavailable, thread, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        conversation.LastIntent = draft.Intent;

        var policy = await db.AutomationPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.BusinessId == conversation.BusinessId, cancellationToken);
        var mayAnswerAlone = allowAutoSend
                             && policy is { AutomationEnabled: true, AutoReplyToMessages: true }
                             && conversation.StatusId == StatusIds.Conversation.Open
                             && !draft.NeedsOwner
                             && draft.Reply is not null
                             && draft.Confidence >= settings.MinAutoReplyConfidence
                             && messenger.CanReplyFreely(conversation);

        if (mayAnswerAlone)
        {
            try
            {
                await messenger.SendAsync(conversation, new OutgoingMessage(draft.Reply, null, null, MessageAuthor.Ai, null, execution.Id), cancellationToken);
                logger.LogInformation("AI answered WhatsApp conversation {ConversationId} ({Intent}).", conversationId, draft.Intent);
                return;
            }
            catch (AppException ex)
            {
                logger.LogWarning("Automatic WhatsApp reply not sent for {ConversationId}: {Message}", conversationId, ex.Message);
            }
        }

        conversation.SuggestedReply = Truncate(draft.Reply, 2000);
        conversation.SuggestedByExecutionId = draft.Reply is null ? null : execution.Id;
        if (draft.NeedsOwner)
            await HandOffAsync(conversation, draft.HandoffReason ?? "The AI is not sure.", thread, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandOffAsync(Conversation conversation, string reason, ChatThread thread, CancellationToken cancellationToken)
    {
        var alreadyWaiting = conversation.StatusId == StatusIds.Conversation.WaitingForOwner;
        conversation.StatusId = StatusIds.Conversation.WaitingForOwner;
        conversation.HandoffReason = Truncate(reason, 300);
        if (!alreadyWaiting && options.Value.NotifyOwnerByEmail)
            await NotifyOwnerAsync(conversation, reason, thread.Lines.LastOrDefault(l => l.FromCustomer)?.Text, cancellationToken);
    }

    private async Task NotifyOwnerAsync(Conversation conversation, string reason, string? lastMessage, CancellationToken cancellationToken)
    {
        var business = await db.Businesses.AsNoTracking().FirstAsync(b => b.Id == conversation.BusinessId, cancellationToken);
        var recipients = await db.TenantMemberships.AsNoTracking()
            .Where(m => m.TenantId == business.TenantId && m.StatusId == StatusIds.Membership.Active
                        && (m.Role == MembershipRole.Owner || m.Role == MembershipRole.Admin))
            .Select(m => m.User.Email)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0) return;

        var customer = WebUtility.HtmlEncode(conversation.CustomerName ?? conversation.CustomerPhone);
        var html = $"""
            <div dir="rtl" style="font-family:Tahoma,Arial,sans-serif;font-size:15px">
              <p><b>زبون على واتساب بحاجة لرد منك</b> - {WebUtility.HtmlEncode(business.Name)}</p>
              <p>الزبون: {customer} ({WebUtility.HtmlEncode(conversation.CustomerPhone)})</p>
              <p>السبب: {WebUtility.HtmlEncode(reason)}</p>
              <p>آخر رسالة: {WebUtility.HtmlEncode(lastMessage ?? "")}</p>
            </div>
            """;
        try
        {
            await email.SendAsync(new EmailMessage(recipients, $"WhatsApp: {conversation.CustomerName ?? conversation.CustomerPhone} needs you", html), cancellationToken);
        }
        catch (EmailException ex)
        {
            logger.LogWarning("Handoff email for conversation {ConversationId} not sent: {Message}", conversation.Id, ex.Message);
        }
    }

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
