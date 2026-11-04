using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Audience;

namespace SmartMarketingPlatform.Services;

public interface IAudienceSegmentService
{
    Task<PagedResult<AudienceSegmentResponse>> GetAllAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken);
    Task<AudienceSegmentResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<AudienceSegmentResponse> CreateAsync(Guid businessId, AudienceSegmentRequest request, CancellationToken cancellationToken);
    Task<AudienceSegmentResponse> UpdateAsync(Guid businessId, Guid id, AudienceSegmentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}
