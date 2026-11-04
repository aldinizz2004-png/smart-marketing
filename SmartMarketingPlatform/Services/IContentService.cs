using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Content;

namespace SmartMarketingPlatform.Services;

public interface IContentService
{
    Task<PagedResult<ContentItemResponse>> GetAllAsync(Guid businessId, ContentListQuery query, CancellationToken cancellationToken);
    Task<ContentItemResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<ContentItemResponse> CreateAsync(Guid businessId, CreateContentItemRequest request, CancellationToken cancellationToken);
    Task<ContentItemResponse> AddRevisionAsync(Guid businessId, Guid id, RevisionRequest request, CancellationToken cancellationToken);
    Task<ContentItemResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
    Task<ContentItemResponse> ReviewAsync(Guid businessId, Guid id, ReviewRequest request, CancellationToken cancellationToken);
    Task<ContentItemResponse> ApplyRecommendationAsync(Guid businessId, Guid recommendationId, ApplyRecommendationRequest request, CancellationToken cancellationToken);
}
