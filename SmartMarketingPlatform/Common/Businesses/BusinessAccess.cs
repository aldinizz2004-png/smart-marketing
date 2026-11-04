using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Common.Businesses;

public sealed class BusinessAccess(MarketingDbContext db, ICurrentUser currentUser) : IBusinessAccess
{
    public async Task<Business> GetAsync(Guid businessId, CancellationToken cancellationToken) =>
        await db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.TenantId == currentUser.TenantId, cancellationToken)
        ?? throw new NotFoundException("Business not found.");

    public async Task<Business> GetForWriteAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await GetAsync(businessId, cancellationToken);
        if (business.StatusId == StatusIds.Business.Closed)
            throw new ConflictException("This business is closed; its data is read-only.");
        return business;
    }
}
