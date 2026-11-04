using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

public sealed class PostproxyMetricsProvider(PostproxyClient client, ILogger<PostproxyMetricsProvider> logger) : IMetricsProvider
{
    private static readonly Dictionary<string, MetricCode> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["impressions"] = MetricCode.Impressions,
        ["reach"] = MetricCode.Reach,
        ["likes"] = MetricCode.Likes,
        ["comments"] = MetricCode.Comments,
        ["shares"] = MetricCode.Shares,
        ["saved"] = MetricCode.Saves,
        ["clicks"] = MetricCode.LinkClicks,
        ["views"] = MetricCode.VideoViews,
        ["video_views"] = MetricCode.VideoViews
    };

    public async Task<IReadOnlyDictionary<MetricCode, decimal>> GetMetricsAsync(MetricsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await client.GetStatsAsync(request.ExternalPostId, cancellationToken);
            var post = stats[request.ExternalPostId] as JsonObject ?? (stats["data"] as JsonObject)?[request.ExternalPostId] as JsonObject;
            var latest = (post?["platforms"] as JsonArray)?.OfType<JsonObject>()
                .SelectMany(p => (p["records"] as JsonArray)?.OfType<JsonObject>() ?? [])
                .OrderByDescending(r => r["recorded_at"]?.ToString())
                .FirstOrDefault();

            return ToMetrics(latest?["stats"] as JsonObject);
        }
        catch (PostproxyException ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.IsTransient)
        {
            logger.LogWarning("No stats for post {PostId}: {Error}", request.ExternalPostId, ex.Message);
            return new Dictionary<MetricCode, decimal>();
        }
    }

    public static IReadOnlyDictionary<MetricCode, decimal> ToMetrics(JsonObject? stats)
    {
        var result = new Dictionary<MetricCode, decimal>();
        foreach (var (name, value) in stats ?? [])
        {
            if (!Map.TryGetValue(name, out var code) || value is null) continue;
            if (decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                result[code] = number;
        }
        return result;
    }
}
