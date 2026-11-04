using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Integrations;

public class PostproxyMappingTests
{
    [Theory]
    [InlineData("instagram", "instagram")]
    [InlineData("facebook", "facebook")]
    [InlineData("x", "twitter")]
    public void Maps_our_platform_codes_to_postproxy(string ours, string theirs)
    {
        Assert.Equal(theirs, PostproxyPlatforms.ToPostproxy(ours));
        Assert.Equal(ours, PostproxyPlatforms.FromPostproxy(theirs));
    }

    [Theory]
    [InlineData("snapchat")]
    [InlineData("website")]
    public void Platforms_without_an_integration_are_not_mapped(string platformCode)
    {
        Assert.Null(PostproxyPlatforms.ToPostproxy(platformCode));
    }

    [Fact]
    public void Maps_normalized_stats_to_metric_codes()
    {
        var stats = JsonNode.Parse("""{"impressions":1200,"reach":950,"likes":80,"comments":12,"shares":5,"saved":9,"clicks":3,"unknown_metric":7}""")!.AsObject();

        var metrics = PostproxyMetricsProvider.ToMetrics(stats);

        Assert.Equal(1200m, metrics[MetricCode.Impressions]);
        Assert.Equal(950m, metrics[MetricCode.Reach]);
        Assert.Equal(9m, metrics[MetricCode.Saves]);
        Assert.Equal(3m, metrics[MetricCode.LinkClicks]);
        Assert.Equal(7, metrics.Count);
    }

    [Fact]
    public void Missing_stats_give_no_metrics()
    {
        Assert.Empty(PostproxyMetricsProvider.ToMetrics(null));
    }
}
