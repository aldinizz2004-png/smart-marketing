using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Knowledge;

namespace SmartMarketingPlatform.Services;

public interface IKnowledgeSourceService
{
    Task<PagedResult<KnowledgeSourceResponse>> GetAllAsync(Guid businessId, KnowledgeListQuery query, CancellationToken cancellationToken);
    Task<KnowledgeSourceResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<KnowledgeSourceResponse> CreateAsync(Guid businessId, KnowledgeSourceRequest request, CancellationToken cancellationToken);
    Task<KnowledgeSourceResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}
