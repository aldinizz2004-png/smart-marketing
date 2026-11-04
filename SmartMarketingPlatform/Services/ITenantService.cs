using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Tenants;

namespace SmartMarketingPlatform.Services;

// Always works on the tenant of the access token (ICurrentUser.TenantId).
public interface ITenantService
{
    Task<TenantResponse> GetCurrentAsync(CancellationToken cancellationToken);
    Task<TenantResponse> UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken);

    Task<PagedResult<MemberResponse>> GetMembersAsync(PageQuery page, CancellationToken cancellationToken);
    Task<MemberResponse> GetMemberAsync(Guid userId, CancellationToken cancellationToken);
    Task<MemberResponse> AddMemberAsync(AddMemberRequest request, CancellationToken cancellationToken);
    Task<MemberResponse> UpdateMemberAsync(Guid userId, UpdateMemberRequest request, CancellationToken cancellationToken);
}
