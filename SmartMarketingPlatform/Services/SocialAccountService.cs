using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Secrets;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Social;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

// Accounts are never deleted (publications point to them): they are disconnected.
public sealed class SocialAccountService(
    MarketingDbContext db,
    IBusinessAccess access,
    ISecretStore secrets,
    StatusCatalog statuses) : ISocialAccountService
{
    public async Task<IReadOnlyList<SocialAccountResponse>> GetAllAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var accounts = await db.SocialAccounts.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderBy(s => s.PlatformCode).ThenBy(s => s.Handle)
            .ToListAsync(cancellationToken);
        return accounts.Select(ToResponse).ToList();
    }

    public async Task<SocialAccountResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken));
    }

    public async Task<(SocialAccountResponse Account, bool Created)> ConnectAsync(
        Guid businessId, ConnectSocialAccountRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var platformCode = request.PlatformCode!.Trim().ToLowerInvariant();
        var platform = await db.Platforms.AsNoTracking().FirstOrDefaultAsync(p => p.Code == platformCode && p.IsActive, cancellationToken)
                       ?? throw new BadRequestException($"Unknown platform '{platformCode}'. See /api/platforms.");
        if (!platform.SupportsPublishing)
            throw new BadRequestException($"Publishing to '{platformCode}' is not supported.");

        var externalId = request.ExternalAccountId!.Trim();

        // The same page can be connected once in the whole system (unique index on platform + external id).
        var existing = await db.SocialAccounts
            .FirstOrDefaultAsync(s => s.PlatformCode == platformCode && s.ExternalAccountId == externalId, cancellationToken);
        if (existing is not null && (existing.BusinessId != businessId || existing.StatusId != StatusIds.SocialAccount.Disconnected))
            throw new ConflictException("This account is already connected.");

        var account = existing ?? new SocialAccount { BusinessId = businessId, PlatformCode = platformCode, ExternalAccountId = externalId };
        account.Handle = request.Handle?.Trim().TrimStart('@');
        await ReplaceCredentialAsync(account, request.AccessToken!, cancellationToken);
        account.StatusId = StatusIds.SocialAccount.Connected;

        if (existing is null) db.SocialAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(account), existing is null);
    }

    public async Task<SocialAccountResponse> UpdateCredentialsAsync(Guid businessId, Guid id, UpdateCredentialsRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var account = await LoadAsync(businessId, id, cancellationToken);

        await ReplaceCredentialAsync(account, request.AccessToken!, cancellationToken);
        account.StatusId = StatusIds.SocialAccount.Connected;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(account);
    }

    // Idempotent: disconnecting twice is fine.
    public async Task<SocialAccountResponse> DisconnectAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var account = await LoadAsync(businessId, id, cancellationToken);

        if (account.CredentialRef is not null)
        {
            await secrets.DeleteAsync(account.CredentialRef, cancellationToken);
            account.CredentialRef = null;
        }
        account.StatusId = StatusIds.SocialAccount.Disconnected;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(account);
    }

    private async Task ReplaceCredentialAsync(SocialAccount account, string accessToken, CancellationToken cancellationToken)
    {
        var oldRef = account.CredentialRef;
        account.CredentialRef = await secrets.SaveAsync(accessToken.Trim(), cancellationToken);
        if (oldRef is not null)
            await secrets.DeleteAsync(oldRef, cancellationToken);
    }

    private async Task<SocialAccount> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.SocialAccounts.FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Social account not found.");

    private SocialAccountResponse ToResponse(SocialAccount s) => new(
        s.Id, s.BusinessId, s.PlatformCode, s.ExternalAccountId, s.Handle,
        s.CredentialRef is not null, statuses.Describe(s.StatusId), s.CreatedAt);
}
