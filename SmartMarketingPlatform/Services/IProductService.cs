using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Products;

namespace SmartMarketingPlatform.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetAllAsync(Guid businessId, ProductListQuery query, CancellationToken cancellationToken);
    Task<ProductResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateAsync(Guid businessId, ProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateAsync(Guid businessId, Guid id, ProductRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}
