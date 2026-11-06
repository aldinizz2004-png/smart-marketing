using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Auth;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed class AuthService(
    MarketingDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    ITokenService tokens,
    IOptions<JwtOptions> jwtOptions,
    IHttpContextAccessor httpContext,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "The refresh token is invalid or expired. Log in again.";

    // Used to spend the same hashing time when the email does not exist,
    // so response time does not reveal which emails are registered.
    private static readonly AppUser DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(DummyUser, "dummy-password-1");

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email!);
        if (await db.AppUsers.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var user = new AppUser
        {
            Email = email,
            FullName = request.FullName!.Trim(),
            PreferredLanguage = request.PreferredLanguage ?? "ar"
        };
        db.AppUsers.Add(user);                       // EF assigns a sequential Guid here
        user.AuthSubject = $"local|{user.Id:N}";
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);

        var tenant = new Tenant { Name = request.TenantName!.Trim(), StatusId = StatusIds.Tenant.Active };
        db.Tenants.Add(tenant);

        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = MembershipRole.Owner,
            StatusId = StatusIds.Membership.Active
        });

        // User, tenant, membership and the first refresh token are saved in one transaction.
        return await IssueAsync(user, tenant, MembershipRole.Owner, familyId: Guid.NewGuid(), replaces: null, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email!);
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user?.PasswordHash is null)
        {
            passwordHasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password!);
            throw new UnauthorizedException(InvalidCredentials);
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException(InvalidCredentials);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);

        // Only active memberships of active tenants can be used.
        var membership = await ActiveMemberships(user.Id)
            .Where(m => request.TenantId == null || m.TenantId == request.TenantId)
            .OrderBy(m => m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException(request.TenantId is null
                ? "This account has no active tenant."
                : "You are not an active member of this tenant.");

        return await IssueAsync(user, membership.Tenant, membership.Role, familyId: Guid.NewGuid(), replaces: null, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = RefreshTokenValue.Hash(request.RefreshToken!);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
                     ?? throw new UnauthorizedException(InvalidRefreshToken);

        if (stored.RevokedAt is { } revokedAt)
        {
            // An old token used again after the grace time: someone else has a copy. End that whole session.
            var parallelUse = stored.RevokeReason == RevokeReasons.Rotated
                              && now - revokedAt < TimeSpan.FromSeconds(jwtOptions.Value.RefreshReuseGraceSeconds);
            if (!parallelUse)
            {
                var revoked = await RevokeFamilyAsync(stored.FamilyId, RevokeReasons.ReuseDetected, now, cancellationToken);
                logger.LogWarning("Refresh token reuse detected for user {UserId}; {Count} token(s) of the session revoked.", stored.UserId, revoked);
            }
            throw new UnauthorizedException(InvalidRefreshToken);
        }
        if (stored.ExpiresAt <= now)
            throw new UnauthorizedException(InvalidRefreshToken);

        // The role and the access are read again: a removed member cannot refresh their way back in.
        var membership = await ActiveMemberships(stored.UserId)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.TenantId == stored.TenantId, cancellationToken);
        if (membership is null)
        {
            await RevokeFamilyAsync(stored.FamilyId, RevokeReasons.AccessRemoved, now, cancellationToken);
            throw new UnauthorizedException("You are no longer an active member of this tenant.");
        }

        // Only one request can use a token: the update succeeds for the first one only.
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokeReason, RevokeReasons.Rotated), cancellationToken);
        if (claimed == 0)
            throw new UnauthorizedException(InvalidRefreshToken);

        return await IssueAsync(membership.User, membership.Tenant, membership.Role, stored.FamilyId, stored, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        // An unknown token gives the same answer: logout never tells whether a token exists.
        var hash = RefreshTokenValue.Hash(request.RefreshToken!);
        var familyId = await db.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (familyId is { } family)
            await RevokeFamilyAsync(family, RevokeReasons.Logout, clock.GetUtcNow(), cancellationToken);
    }

    public Task<int> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokeReason, RevokeReasons.LogoutAll), cancellationToken);
    }

    // ---- helpers --------------------------------------------------------------------

    private async Task<AuthResponse> IssueAsync(
        AppUser user, Tenant tenant, MembershipRole role, Guid familyId, RefreshToken? replaces, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var (accessToken, expiresAt) = tokens.CreateToken(user, tenant.Id, role);

        var refreshToken = RefreshTokenValue.New();
        var request = httpContext.HttpContext?.Request;
        var row = new RefreshToken
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            FamilyId = familyId,
            TokenHash = RefreshTokenValue.Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
            CreatedByIp = Truncate(request?.HttpContext.Connection.RemoteIpAddress?.ToString(), 64),
            UserAgent = Truncate(request?.Headers.UserAgent.ToString(), 300)
        };
        db.RefreshTokens.Add(row);
        if (replaces is not null)
            replaces.ReplacedByTokenId = row.Id;

        // Expired tokens of this user are no longer useful, not even for reuse detection.
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            "Bearer",
            expiresAt,
            refreshToken,
            row.ExpiresAt,
            new UserSummary(user.Id, user.Email, user.FullName, user.PreferredLanguage),
            new TenantSummary(tenant.Id, tenant.Name, role));
    }

    private IQueryable<TenantMembership> ActiveMemberships(Guid userId) =>
        db.TenantMemberships
            .Include(m => m.Tenant)
            .Where(m => m.UserId == userId
                        && m.StatusId == StatusIds.Membership.Active
                        && m.Tenant.StatusId == StatusIds.Tenant.Active);

    private Task<int> RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokeReason, reason), cancellationToken);

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];

    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static class RevokeReasons
    {
        public const string Rotated = "rotated";
        public const string Logout = "logout";
        public const string LogoutAll = "logout_all";
        public const string ReuseDetected = "reuse_detected";
        public const string AccessRemoved = "access_removed";
    }
}
