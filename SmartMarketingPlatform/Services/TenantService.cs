using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Tenants;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed class TenantService(
    MarketingDbContext db,
    ICurrentUser currentUser,
    StatusCatalog statuses,
    ISubscriptionService subscriptions) : ITenantService
{
    public async Task<TenantResponse> GetCurrentAsync(CancellationToken cancellationToken)
        => await ToResponseAsync(await LoadTenantAsync(cancellationToken), cancellationToken);

    public async Task<TenantResponse> UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await LoadTenantAsync(cancellationToken);
        tenant.Name = request.Name!.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(tenant, cancellationToken);
    }

    public async Task<PagedResult<MemberResponse>> GetMembersAsync(PageQuery page, CancellationToken cancellationToken)
    {
        var result = await db.TenantMemberships.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.TenantId == currentUser.TenantId)
            .OrderBy(m => m.JoinedAt).ThenBy(m => m.UserId)
            .ToPagedResultAsync(page, cancellationToken);

        return result.Map(ToResponse);
    }

    public async Task<MemberResponse> GetMemberAsync(Guid userId, CancellationToken cancellationToken)
        => ToResponse(await LoadMembershipAsync(userId, cancellationToken));

    public async Task<MemberResponse> AddMemberAsync(AddMemberRequest request, CancellationToken cancellationToken)
    {
        if (request.Role == MembershipRole.Owner)
            throw new BadRequestException("A tenant has exactly one owner; choose another role.");

        var email = AuthService.NormalizeEmail(request.Email!);
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
                   ?? throw new NotFoundException("No registered user has this email. Ask them to register first.");

        if (await db.TenantMemberships.AnyAsync(m => m.TenantId == currentUser.TenantId && m.UserId == user.Id, cancellationToken))
            throw new ConflictException("This user is already a member of the tenant.");

        var membership = new TenantMembership
        {
            TenantId = currentUser.TenantId,
            UserId = user.Id,
            Role = request.Role!.Value,
            StatusId = StatusIds.Membership.Active,
            User = user
        };
        db.TenantMemberships.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(membership);
    }

    public async Task<MemberResponse> UpdateMemberAsync(Guid userId, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        if (request.Role is null && request.IsActive is null)
            throw new BadRequestException("Send at least one field to update: role or isActive.");
        if (request.Role == MembershipRole.Owner)
            throw new BadRequestException("Ownership cannot be given through this endpoint.");
        if (userId == currentUser.UserId)
            throw new BadRequestException("You cannot change your own membership.");

        var membership = await LoadMembershipAsync(userId, cancellationToken);
        if (membership.Role == MembershipRole.Owner)
            throw new ForbiddenException("The owner's membership cannot be changed.");

        if (request.Role is not null) membership.Role = request.Role.Value;
        if (request.IsActive is not null)
            membership.StatusId = request.IsActive.Value ? StatusIds.Membership.Active : StatusIds.Membership.Disabled;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(membership);
    }

    private async Task<Tenant> LoadTenantAsync(CancellationToken cancellationToken) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.Id == currentUser.TenantId, cancellationToken)
        ?? throw new NotFoundException("The tenant of this token no longer exists.");

    // Filtering by the token's tenant: a user id from another tenant simply returns 404.
    private async Task<TenantMembership> LoadMembershipAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.TenantMemberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.TenantId == currentUser.TenantId && m.UserId == userId, cancellationToken)
        ?? throw new NotFoundException("Member not found.");

    private async Task<TenantResponse> ToResponseAsync(Tenant tenant, CancellationToken cancellationToken) => new(
        tenant.Id,
        tenant.Name,
        statuses.Describe(tenant.StatusId),
        currentUser.Role,
        tenant.CreatedAt,
        await subscriptions.FindCurrentAsync(cancellationToken));

    private MemberResponse ToResponse(TenantMembership m) => new(
        m.UserId,
        m.User.Email,
        m.User.FullName,
        m.Role,
        statuses.Describe(m.StatusId),
        m.JoinedAt);
}
