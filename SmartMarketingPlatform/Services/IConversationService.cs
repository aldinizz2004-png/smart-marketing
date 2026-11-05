using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Conversations;

namespace SmartMarketingPlatform.Services;

public interface IConversationService
{
    Task<PagedResult<ConversationSummaryResponse>> GetAllAsync(Guid businessId, ConversationListQuery query, CancellationToken cancellationToken);
    Task<ConversationResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<ConversationResponse> SendAsync(Guid businessId, Guid id, SendMessageRequest request, CancellationToken cancellationToken);
    Task<ConversationResponse> SuggestAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<ConversationResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
}
