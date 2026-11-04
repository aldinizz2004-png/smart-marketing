using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Businesses;
using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.Services;

public interface IBusinessService
{
    Task<PagedResult<BusinessResponse>> GetAllAsync(BusinessListQuery query, CancellationToken cancellationToken);
    Task<BusinessResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<BusinessResponse> CreateAsync(BusinessRequest request, CancellationToken cancellationToken);
    Task<BusinessResponse> UpdateAsync(Guid id, BusinessRequest request, CancellationToken cancellationToken);
    Task<BusinessResponse> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
}
