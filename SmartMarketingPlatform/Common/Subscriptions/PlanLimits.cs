using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Subscriptions;

public sealed record PlanInfo(SubscriptionPlan Plan, int MaxPostsPerDay);

// The daily post limit that really applies: the owner's own limit, capped by the plan.
// LimitedByPlan tells the owner WHY a day is full (upgrade the plan, or raise their own limit).
public sealed record DailyPostLimit(int Value, int PlanMax, SubscriptionPlan Plan, bool LimitedByPlan)
{
    public static DailyPostLimit Combine(int? ownerLimit, PlanInfo plan)
    {
        var limitedByPlan = ownerLimit is null || ownerLimit >= plan.MaxPostsPerDay;
        return new DailyPostLimit(limitedByPlan ? plan.MaxPostsPerDay : ownerLimit!.Value, plan.MaxPostsPerDay, plan.Plan, limitedByPlan);
    }

    public string FullDayMessage => LimitedByPlan
        ? $"Your {Plan} plan allows {PlanMax} posts per day and that day is full. Upgrade the plan to publish more."
        : $"The owner's limit of {Value} posts per day is already reached for that day.";
}

public interface IPlanLimits
{
    // The plan of the subscription that grants access now (Basic when there is none).
    Task<PlanInfo> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<PlanInfo> GetForBusinessAsync(Guid businessId, CancellationToken cancellationToken);
    PlanInfo Describe(SubscriptionPlan plan);
}

public sealed class PlanLimits(MarketingDbContext db, IOptions<SubscriptionPricingOptions> pricing, TimeProvider clock) : IPlanLimits
{
    public async Task<PlanInfo> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var plan = await db.Subscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.StatusId == StatusIds.Subscription.Active && s.StartsAt <= now && s.EndsAt > now)
            .OrderByDescending(s => s.EndsAt)
            .Select(s => (SubscriptionPlan?)s.Plan)
            .FirstOrDefaultAsync(cancellationToken);
        return Describe(plan ?? SubscriptionPlan.Basic);
    }

    public async Task<PlanInfo> GetForBusinessAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = await db.Businesses.Where(b => b.Id == businessId).Select(b => b.TenantId).FirstAsync(cancellationToken);
        return await GetForTenantAsync(tenantId, cancellationToken);
    }

    // A plan missing from configuration gets the safest limit rather than no limit.
    public PlanInfo Describe(SubscriptionPlan plan) =>
        new(plan, pricing.Value.Plans.TryGetValue(plan, out var options) ? options.MaxPostsPerDay : 1);
}
