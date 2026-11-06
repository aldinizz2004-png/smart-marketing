using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Common.Subscriptions;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Subscriptions;

public class DailyPostLimitTests
{
    private static readonly PlanInfo Basic = new(SubscriptionPlan.Basic, 3);

    [Fact]
    public void Without_an_owner_limit_the_plan_decides()
    {
        var limit = DailyPostLimit.Combine(null, Basic);

        Assert.Equal(3, limit.Value);
        Assert.True(limit.LimitedByPlan);
    }

    [Fact]
    public void A_lower_owner_limit_wins()
    {
        var limit = DailyPostLimit.Combine(2, Basic);

        Assert.Equal(2, limit.Value);
        Assert.False(limit.LimitedByPlan);
        Assert.Contains("owner's limit of 2", limit.FullDayMessage);
    }

    [Fact]
    public void A_higher_owner_limit_is_capped_by_the_plan()
    {
        // e.g. the owner chose 10 on Pro, then the tenant went back to Basic.
        var limit = DailyPostLimit.Combine(10, Basic);

        Assert.Equal(3, limit.Value);
        Assert.True(limit.LimitedByPlan);
        Assert.Contains("Basic plan allows 3", limit.FullDayMessage);
    }

    [Fact]
    public void Plan_limits_come_from_configuration()
    {
        var options = Options.Create(new SubscriptionPricingOptions
        {
            Plans = new()
            {
                [SubscriptionPlan.Basic] = new PlanOptions { MaxPostsPerDay = 3 },
                [SubscriptionPlan.Pro] = new PlanOptions { MaxPostsPerDay = 10 }
            }
        });
        var plans = new PlanLimits(null!, options, TimeProvider.System);

        Assert.Equal(10, plans.Describe(SubscriptionPlan.Pro).MaxPostsPerDay);
        Assert.Equal(1, plans.Describe(SubscriptionPlan.Business).MaxPostsPerDay);   // missing from configuration: the safest limit
    }
}
