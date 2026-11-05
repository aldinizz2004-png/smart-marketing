using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Competitors;

public class CompetitorStatsTests
{
    private static readonly TimeZoneInfo Hebron = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hebron");
    private static readonly Guid Source = Guid.NewGuid();

    private static StatPost Post(ContentFormat format, decimal rate, int utcHour, int day, string caption = "منشور", Guid? source = null) => new(
        Guid.NewGuid(), source ?? Source, "rival_cafe", format, PostText.DetectHook(caption),
        new DateTimeOffset(2026, 10, day, utcHour, 0, 0, TimeSpan.Zero), caption, rate, 100);

    [Fact]
    public void Compares_each_format_with_the_overall_average()
    {
        var facts = CompetitorStats.Build(
        [
            Post(ContentFormat.Reel, 0.09m, 17, 1),
            Post(ContentFormat.Reel, 0.09m, 17, 3),
            Post(ContentFormat.SingleImage, 0.03m, 9, 5),
            Post(ContentFormat.SingleImage, 0.03m, 9, 7)
        ], Hebron);

        var best = facts.ByFormat[0];
        Assert.Equal("Reel", best.Key);
        Assert.Equal(0.09m, best.AverageScore);
        Assert.Equal(1.5m, best.Lift);
        Assert.Equal(0.06m, facts.AverageScore);
    }

    [Fact]
    public void Times_are_grouped_in_the_business_time_zone()
    {
        // 17:00 UTC in October is 20:00 in Palestine (UTC+3).
        var facts = CompetitorStats.Build([Post(ContentFormat.Reel, 0.05m, 17, 1), Post(ContentFormat.Reel, 0.05m, 17, 2)], Hebron);

        Assert.Equal("18-22", Assert.Single(facts.ByTimeOfDay).Key);
    }

    [Fact]
    public void Counts_posts_per_week_for_each_source()
    {
        var other = Guid.NewGuid();
        var posts = Enumerable.Range(1, 14)
            .Select(d => Post(ContentFormat.Reel, 0.05m, 17, d, source: d % 2 == 0 ? other : Source))
            .ToList();

        var facts = CompetitorStats.Build(posts, Hebron);

        Assert.Equal(2, facts.Sources);
        Assert.Equal(3.5m, facts.PostsPerWeekPerSource);
    }

    [Fact]
    public void Falls_back_to_interactions_when_a_follower_count_is_missing()
    {
        var withoutRate = Post(ContentFormat.Reel, 0, 17, 1) with { EngagementRate = null };

        var facts = CompetitorStats.Build([withoutRate, Post(ContentFormat.Reel, 0.05m, 17, 2)], Hebron);

        Assert.StartsWith("interactions", facts.ScoreMeaning);
        Assert.Equal(100m, facts.AverageScore);
    }

    [Fact]
    public void Top_posts_carry_only_a_short_excerpt_of_the_caption()
    {
        var longCaption = string.Join(" ", Enumerable.Repeat("كلمة", 120));

        var facts = CompetitorStats.Build([Post(ContentFormat.Reel, 0.05m, 17, 1, longCaption)], Hebron);

        Assert.True(facts.TopPosts[0].Excerpt!.Length <= 161);
    }
}
