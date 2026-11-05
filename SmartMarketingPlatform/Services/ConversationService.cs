using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Conversations;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// The owner's WhatsApp inbox: read conversations, answer them, or hand them back to the AI.
public sealed class ConversationService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    IConversationMessenger messenger,
    IWhatsAppInboxService inbox,
    StatusCatalog statuses,
    IOptions<WhatsAppOptions> options,
    TimeProvider clock) : IConversationService
{
    private const int MaxMessages = 100;

    public async Task<PagedResult<ConversationSummaryResponse>> GetAllAsync(Guid businessId, ConversationListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var conversations = db.Conversations.AsNoTracking().Where(c => c.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("Conversation", query.Status);
            conversations = conversations.Where(c => c.StatusId == statusId);
        }
        if (query.Intent is { } intent)
            conversations = conversations.Where(c => c.LastIntent == intent);

        var page = await Project(conversations.OrderByDescending(c => c.LastMessageAt).ThenBy(c => c.Id)).ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToSummary);
    }

    public async Task<ConversationResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    public async Task<ConversationResponse> SendAsync(Guid businessId, Guid id, SendMessageRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var conversation = await LoadAsync(businessId, id, cancellationToken);

        var hasText = !string.IsNullOrWhiteSpace(request.Text);
        var hasTemplate = !string.IsNullOrWhiteSpace(request.TemplateName);
        if (hasText && hasTemplate)
            throw new BadRequestException("Send either text or templateName, not both.");

        var text = hasText ? request.Text : hasTemplate ? null : conversation.SuggestedReply;
        if (!hasTemplate && string.IsNullOrWhiteSpace(text))
            throw new BadRequestException("Write a message, or ask the AI for a suggestion first.");

        await messenger.SendAsync(conversation,
            new OutgoingMessage(text, request.TemplateName, request.LanguageCode, MessageAuthor.User, currentUser.UserId, null), cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    public async Task<ConversationResponse> SuggestAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        await LoadAsync(businessId, id, cancellationToken);

        await inbox.RespondAsync(id, allowAutoSend: false, cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    // Open = the AI handles it again, Closed = done. WaitingForOwner is set by the system.
    public async Task<ConversationResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var conversation = await LoadAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("Conversation", request.Status!);
        if (target is not (StatusIds.Conversation.Open or StatusIds.Conversation.Closed))
            throw new BadRequestException("A conversation can only be set to 'Open' or 'Closed'.");

        conversation.StatusId = target;
        conversation.HandoffReason = null;
        await db.SaveChangesAsync(cancellationToken);
        return await LoadResponseAsync(businessId, id, cancellationToken);
    }

    private async Task<Conversation> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.Conversations.Include(c => c.SocialAccount)
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Conversation not found.");

    private async Task<ConversationResponse> LoadResponseAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var row = await Project(db.Conversations.AsNoTracking().Where(c => c.Id == id && c.BusinessId == businessId))
                      .FirstOrDefaultAsync(cancellationToken)
                  ?? throw new NotFoundException("Conversation not found.");

        var messages = await db.ConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt).Take(MaxMessages)
            .Select(m => new MessageResponse(m.Id, m.Direction, m.Author, m.MessageType, m.Body, m.DeliveryStatus, m.Error, m.SentByUserId, m.SentAt))
            .ToListAsync(cancellationToken);
        messages.Reverse();

        return new ConversationResponse(ToSummary(row), row.SuggestedReply, messages);
    }

    private static IQueryable<Row> Project(IQueryable<Conversation> conversations) => conversations.Select(c => new Row(
        c.Id, c.CustomerPhone, c.CustomerName, c.StatusId, c.LastIntent, c.HandoffReason, c.SuggestedReply,
        c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Body).FirstOrDefault(),
        c.LastMessageAt, c.LastCustomerMessageAt));

    private ConversationSummaryResponse ToSummary(Row r)
    {
        var windowEnds = r.LastCustomerMessageAt?.AddHours(options.Value.ReplyWindowHours);
        return new ConversationSummaryResponse(
            r.Id, r.CustomerPhone, r.CustomerName, statuses.Describe(r.StatusId), r.LastIntent, r.HandoffReason,
            r.SuggestedReply is not null, r.LastMessage, r.LastMessageAt, windowEnds > clock.GetUtcNow(), windowEnds);
    }

    private sealed record Row(
        Guid Id, string CustomerPhone, string? CustomerName, int StatusId, ConversationIntent? LastIntent, string? HandoffReason,
        string? SuggestedReply, string? LastMessage, DateTimeOffset LastMessageAt, DateTimeOffset? LastCustomerMessageAt);
}
