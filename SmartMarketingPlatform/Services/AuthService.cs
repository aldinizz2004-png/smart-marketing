using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    ITokenService tokens) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";

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

        // One SaveChanges = one transaction: user, tenant and membership are created together or not at all.
        await db.SaveChangesAsync(cancellationToken);

        return Issue(user, tenant, MembershipRole.Owner);
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
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
            await db.SaveChangesAsync(cancellationToken);
        }

        // Only active memberships of active tenants can be used.
        var membership = await db.TenantMemberships
            .Include(m => m.Tenant)
            .Where(m => m.UserId == user.Id
                        && m.StatusId == StatusIds.Membership.Active
                        && m.Tenant.StatusId == StatusIds.Tenant.Active)
            .Where(m => request.TenantId == null || m.TenantId == request.TenantId)
            .OrderBy(m => m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException(request.TenantId is null
                ? "This account has no active tenant."
                : "You are not an active member of this tenant.");

        return Issue(user, membership.Tenant, membership.Role);
    }

    private AuthResponse Issue(AppUser user, Tenant tenant, MembershipRole role)
    {
        var (token, expiresAt) = tokens.CreateToken(user, tenant.Id, role);
        return new AuthResponse(
            token,
            "Bearer",
            expiresAt,
            new UserSummary(user.Id, user.Email, user.FullName, user.PreferredLanguage),
            new TenantSummary(tenant.Id, tenant.Name, role));
    }

    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
