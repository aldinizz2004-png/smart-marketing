using System.Net;
using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Common.Integrations.Instagram;
using SmartMarketingPlatform.Enums;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.UnitTests.Competitors;

public class CompetitorSourceTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly CompetitorTarget Target = new("instagram", "rival_cafe", "https://instagram.com/rival_cafe");

    [Fact]
    public void Maps_a_business_discovery_answer()
    {
        var root = JsonNode.Parse("""
            {"business_discovery":{"username":"rival_cafe","name":"Rival Cafe","followers_count":5200,
              "media":{"data":[
                {"id":"1","caption":"Reel!","media_type":"VIDEO","media_product_type":"REELS","permalink":"https://instagram.com/reel/1","timestamp":"2026-10-01T18:20:00+0000","like_count":300,"comments_count":40},
                {"id":"2","media_type":"CAROUSEL_ALBUM","timestamp":"2026-10-02T09:00:00+0000","comments_count":3},
                {"caption":"no id, skipped"}
              ]}}}
            """)!.AsObject();

        var snapshot = InstagramDiscoverySource.Map(root);

        Assert.Equal("Rival Cafe", snapshot.DisplayName);
        Assert.Equal(5200, snapshot.Followers);
        Assert.Equal(2, snapshot.Posts.Count);
        Assert.Equal(ContentFormat.Reel, snapshot.Posts[0].Format);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 18, 20, 0, TimeSpan.Zero), snapshot.Posts[0].PostedAt);
        Assert.Equal(ContentFormat.Carousel, snapshot.Posts[1].Format);
        Assert.Null(snapshot.Posts[1].Likes);   // hidden like counts stay unknown, not zero
    }

    [Theory]
    [InlineData("IMAGE", "FEED", ContentFormat.SingleImage)]
    [InlineData("VIDEO", "FEED", ContentFormat.Video)]
    [InlineData("VIDEO", "REELS", ContentFormat.Reel)]
    [InlineData("CAROUSEL_ALBUM", null, ContentFormat.Carousel)]
    public void Maps_instagram_media_types(string mediaType, string? productType, ContentFormat expected)
    {
        Assert.Equal(expected, InstagramDiscoverySource.ToFormat(mediaType, productType));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 100, "not_found", true)]
    [InlineData(HttpStatusCode.BadRequest, 190, "auth_failed", false)]
    [InlineData(HttpStatusCode.Forbidden, 4, "rate_limited", false)]
    [InlineData(HttpStatusCode.InternalServerError, 2, "unavailable", false)]
    public void Graph_errors_say_whether_the_account_itself_is_the_problem(HttpStatusCode status, int code, string expected, bool sourceProblem)
    {
        var error = InstagramDiscoverySource.ToError(status, $$$"""{"error":{"message":"x","code":{{{code}}}}}""");

        Assert.Equal(expected, error.Code);
        Assert.Equal(sourceProblem, error.SourceProblem);
    }

    [Fact]
    public async Task Fake_source_gives_the_same_posts_for_the_same_handle()
    {
        var source = new FakeCompetitorSource(new FixedClock(new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero)));

        var first = await source.FetchAsync(Target, 25, CancellationToken.None);
        var second = await source.FetchAsync(Target, 25, CancellationToken.None);

        Assert.Equal(12, first.Posts.Count);
        Assert.Equal(first.Posts.Select(p => p.Likes), second.Posts.Select(p => p.Likes));
        Assert.Contains(first.Posts, p => p.Format == ContentFormat.Reel);
    }

    [Fact]
    public async Task Fake_source_can_simulate_an_unreadable_account()
    {
        var source = new FakeCompetitorSource(TimeProvider.System);

        var error = await Assert.ThrowsAsync<CompetitorSourceException>(() =>
            source.FetchAsync(Target with { Handle = "notfound_shop" }, 25, CancellationToken.None));

        Assert.True(error.SourceProblem);
    }

    [Fact]
    public void Engagement_rate_needs_a_follower_count()
    {
        var post = new CompetitorPost("1", null, null, ContentFormat.Reel, null, 90, 10, null);

        Assert.Equal(0.02m, CompetitorService.EngagementRate(post, 5_000));
        Assert.Null(CompetitorService.EngagementRate(post, null));
        Assert.Null(CompetitorService.EngagementRate(post with { Likes = null, Comments = null }, 5_000));
    }
}
