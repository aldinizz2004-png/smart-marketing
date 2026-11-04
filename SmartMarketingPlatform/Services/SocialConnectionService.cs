using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Social;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

// The owner connects an account by logging in on the platform itself (OAuth through Postproxy).
// The state parameter is encrypted and expires, so a callback cannot be forged for another business.
public sealed class SocialConnectionService(
    MarketingDbContext db,
    IBusinessAccess access,
    PostproxyClient client,
    IOptions<PostproxyOptions> options,
    IDataProtectionProvider protection,
    TimeProvider clock,
    ILogger<SocialConnectionService> logger) : ISocialConnectionService
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(30);
    private readonly IDataProtector protector = protection.CreateProtector("social-connect-state");

    public async Task<ConnectLinkResponse> CreateConnectLinkAsync(Guid businessId, ConnectLinkRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
            throw new ConflictException("Connecting social accounts is not configured on this server.");

        await access.GetForWriteAsync(businessId, cancellationToken);
        var business = await db.Businesses.FirstAsync(b => b.Id == businessId, cancellationToken);

        var platformCode = request.PlatformCode!.Trim().ToLowerInvariant();
        var platform = await db.Platforms.AsNoTracking().FirstOrDefaultAsync(p => p.Code == platformCode && p.IsActive, cancellationToken)
                       ?? throw new BadRequestException($"Unknown platform '{platformCode}'. See /api/platforms.");
        var providerPlatform = platform.SupportsPublishing ? PostproxyPlatforms.ToPostproxy(platformCode) : null;
        if (providerPlatform is null)
            throw new BadRequestException($"Connecting '{platformCode}' accounts is not supported yet.");

        try
        {
            if (business.SocialGroupId is null)
            {
                business.SocialGroupId = await client.CreateProfileGroupAsync($"smp-{business.Id:N}", cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            var expiresAt = clock.GetUtcNow().Add(LinkLifetime);
            var state = protector.Protect($"{business.Id:N}|{platformCode}|{expiresAt.ToUnixTimeSeconds()}");
            var callback = $"{settings.PublicBaseUrl!.TrimEnd('/')}/api/integrations/postproxy/callback?state={Uri.EscapeDataString(state)}";

            var url = await client.InitializeConnectionAsync(business.SocialGroupId, providerPlatform, callback, cancellationToken);
            return new ConnectLinkResponse(platformCode, url, expiresAt);
        }
        catch (PostproxyException ex)
        {
            throw new BadGatewayException($"The social media provider failed: {ex.Message}");
        }
    }

    public async Task<ConnectionResult> CompleteAsync(string? state, string? profileId, bool failed, string? errorCode, CancellationToken cancellationToken)
    {
        if (!TryReadState(state, out var businessId, out var platformCode))
            return Failure(null, "invalid_or_expired_link");
        if (failed || string.IsNullOrWhiteSpace(profileId))
            return Failure(platformCode, errorCode ?? "connection_failed");

        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business?.SocialGroupId is null)
            return Failure(platformCode, "business_not_found");

        PostproxyProfile profile;
        try
        {
            profile = await client.GetProfileAsync(profileId, cancellationToken);
        }
        catch (PostproxyException ex)
        {
            logger.LogWarning("Could not read Postproxy profile {ProfileId}: {Error}", profileId, ex.Message);
            return Failure(platformCode, "profile_not_found");
        }

        if (profile.ProfileGroupId != business.SocialGroupId || PostproxyPlatforms.FromPostproxy(profile.Platform) != platformCode)
            return Failure(platformCode, "profile_mismatch");

        var account = await db.SocialAccounts
            .FirstOrDefaultAsync(s => s.PlatformCode == platformCode && s.ExternalAccountId == profile.Id, cancellationToken);
        if (account is not null && account.BusinessId != businessId)
            return Failure(platformCode, "account_is_already_connected");

        if (account is null)
        {
            account = new SocialAccount { BusinessId = businessId, PlatformCode = platformCode, ExternalAccountId = profile.Id };
            db.SocialAccounts.Add(account);
        }
        account.Handle = Truncate(profile.Username ?? profile.Name, 100);
        account.StatusId = StatusIds.SocialAccount.Connected;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Business {BusinessId} connected {Platform} account {Handle}", businessId, platformCode, account.Handle);
        return new ConnectionResult(true, platformCode, account.Id, account.Handle, null);
    }

    private bool TryReadState(string? state, out Guid businessId, out string platformCode)
    {
        businessId = Guid.Empty;
        platformCode = string.Empty;
        if (string.IsNullOrWhiteSpace(state)) return false;

        string[] parts;
        try
        {
            parts = protector.Unprotect(state).Split('|');
        }
        catch (CryptographicException)
        {
            return false;
        }

        if (parts.Length != 3 || !Guid.TryParse(parts[0], out businessId) || !long.TryParse(parts[2], out var expires))
            return false;
        platformCode = parts[1];
        return clock.GetUtcNow() <= DateTimeOffset.FromUnixTimeSeconds(expires);
    }

    private static ConnectionResult Failure(string? platformCode, string error) => new(false, platformCode, null, null, error);

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
