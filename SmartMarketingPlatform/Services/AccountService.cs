using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Account;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class AccountService(MarketingDbContext db, ICurrentUser currentUser, StatusCatalog statuses) : IAccountService
{
    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken)
        => ToResponse(await LoadUserAsync(cancellationToken));

    public async Task<MeResponse> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.FullName is null && request.PreferredLanguage is null)
            throw new BadRequestException("Send at least one field to update: fullName or preferredLanguage.");

        var user = await LoadUserAsync(cancellationToken);
        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        if (request.PreferredLanguage is not null) user.PreferredLanguage = request.PreferredLanguage;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    private async Task<AppUser> LoadUserAsync(CancellationToken cancellationToken) =>
        await db.AppUsers
            .Include(u => u.Memberships).ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("The user of this token no longer exists.");

    private MeResponse ToResponse(AppUser user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.PreferredLanguage,
        user.CreatedAt,
        currentUser.TenantId,
        user.Memberships
            .OrderBy(m => m.JoinedAt)
            .Select(m => new MembershipResponse(m.TenantId, m.Tenant.Name, m.Role, statuses.Describe(m.StatusId), m.JoinedAt))
            .ToList());
}
