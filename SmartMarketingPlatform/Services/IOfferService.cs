using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Offers;

namespace SmartMarketingPlatform.Services;

public interface IOfferService
{
    Task<PagedResult<OfferResponse>> GetAllAsync(Guid businessId, OfferListQuery query, CancellationToken cancellationToken);
    Task<OfferResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<OfferResponse> CreateAsync(Guid businessId, OfferRequest request, CancellationToken cancellationToken);
    Task<OfferResponse> UpdateAsync(Guid businessId, Guid id, OfferRequest request, CancellationToken cancellationToken);
    Task<OfferResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}
