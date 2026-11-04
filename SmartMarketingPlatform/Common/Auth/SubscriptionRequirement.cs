using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;

namespace SmartMarketingPlatform.Common.Auth;

// Requirement of the ActiveSubscription policy.
public sealed class SubscriptionRequirement : IAuthorizationRequirement;

// Succeeds only when the token's tenant has an Active subscription whose period includes "now".
// Checked on every request (not stored in the token), so an expired period blocks access immediately.
public sealed class SubscriptionRequirementHandler(MarketingDbContext db, TimeProvider clock)
    : AuthorizationHandler<SubscriptionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SubscriptionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(AppClaims.TenantId), out var tenantId))
            return;

        var now = clock.GetUtcNow();
        var hasAccess = await db.Subscriptions.AnyAsync(s =>
            s.TenantId == tenantId
            && s.StatusId == StatusIds.Subscription.Active
            && s.StartsAt <= now
            && s.EndsAt > now);

        if (hasAccess)
            context.Succeed(requirement);
    }
}
