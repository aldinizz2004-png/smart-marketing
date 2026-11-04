using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Businesses;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class BusinessService(
    MarketingDbContext db,
    ICurrentUser currentUser,
    IBusinessAccess access,
    StatusCatalog statuses) : IBusinessService
{
    // Draft/Active/Paused can move around; Closed is final (history is kept, nothing is deleted).
    private static readonly StatusFlow Flow = new("business", new Dictionary<int, int[]>
    {
        [StatusIds.Business.Draft] = [StatusIds.Business.Active, StatusIds.Business.Closed],
        [StatusIds.Business.Active] = [StatusIds.Business.Paused, StatusIds.Business.Closed],
        [StatusIds.Business.Paused] = [StatusIds.Business.Active, StatusIds.Business.Closed],
        [StatusIds.Business.Closed] = []
    });

    public async Task<PagedResult<BusinessResponse>> GetAllAsync(BusinessListQuery query, CancellationToken cancellationToken)
    {
        var businesses = db.Businesses.AsNoTracking().Where(b => b.TenantId == currentUser.TenantId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("Business", query.Status);
            businesses = businesses.Where(b => b.StatusId == statusId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            businesses = businesses.Where(b => b.Name.Contains(search) || b.Category.Contains(search));
        }

        var page = await businesses
            .OrderBy(b => b.Name).ThenBy(b => b.Id)
            .ToPagedResultAsync(query, cancellationToken);

        return page.Map(ToResponse);
    }

    public async Task<BusinessResponse> GetAsync(Guid id, CancellationToken cancellationToken)
        => ToResponse(await access.GetAsync(id, cancellationToken));

    public async Task<BusinessResponse> CreateAsync(BusinessRequest request, CancellationToken cancellationToken)
    {
        var business = new Business
        {
            TenantId = currentUser.TenantId,
            StatusId = StatusIds.Business.Active
        };
        Apply(business, request);

        db.Businesses.Add(business);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(business);
    }

    public async Task<BusinessResponse> UpdateAsync(Guid id, BusinessRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(id, cancellationToken);
        Apply(business, request);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(business);
    }

    public async Task<BusinessResponse> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetAsync(id, cancellationToken);
        var target = statuses.GetId("Business", request.Status!);

        Flow.EnsureCanMove(business.StatusId, target, statuses);
        business.StatusId = target;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(business);
    }

    // PUT semantics: every field is replaced, optional fields that are not sent become null.
    private static void Apply(Business business, BusinessRequest request)
    {
        var timeZone = request.TimeZoneId!.Trim();
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            throw new BadRequestException($"'{timeZone}' is not a known time zone. Use an IANA id such as 'Asia/Hebron'.");

        business.Name = request.Name!.Trim();
        business.Category = request.Category!.Trim();
        business.CountryCode = request.CountryCode;
        business.City = request.City?.Trim();
        business.Address = request.Address?.Trim();
        business.TimeZoneId = timeZone;
        business.DefaultCurrency = request.DefaultCurrency!;
        business.ContactPhone = request.ContactPhone?.Trim();
        business.Description = request.Description?.Trim();
    }

    private BusinessResponse ToResponse(Business b) => new(
        b.Id, b.Name, b.Category, b.CountryCode, b.City, b.Address, b.TimeZoneId, b.DefaultCurrency,
        b.ContactPhone, b.Description, statuses.Describe(b.StatusId), b.CreatedAt, b.UpdatedAt);
}
