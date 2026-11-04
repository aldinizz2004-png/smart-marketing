using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Common.Businesses;

// Every /api/businesses/{businessId}/... request goes through here first:
// the business must belong to the token's tenant, otherwise the answer is 404.
public interface IBusinessAccess
{
    Task<Business> GetAsync(Guid businessId, CancellationToken cancellationToken);

    // Same check, and the business must not be Closed (closed businesses are read-only).
    Task<Business> GetForWriteAsync(Guid businessId, CancellationToken cancellationToken);
}
