using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Publishing;

// Deterministic, growing numbers: the same post always gets the same curve, numbers rise
// over the first hours and level off, and videos reach more people than single images
// (so the performance report has something real to compare).
public sealed class FakeMetricsProvider : IMetricsProvider
{
    public Task<IReadOnlyDictionary<MetricCode, decimal>> GetMetricsAsync(MetricsRequest request, CancellationToken cancellationToken)
    {
        var seed = (uint)StableHash(request.ExternalPostId);
        var baseReach = 200 + seed % 800;
        var formatBoost = request.Format switch
        {
            ContentFormat.Reel or ContentFormat.Video => 1.8,
            ContentFormat.Carousel => 1.3,
            ContentFormat.Text => 0.6,
            _ => 1.0
        };

        var hours = Math.Max(0, (request.Now - request.PublishedAt).TotalHours);
        var growth = Math.Max(0.05, 1 - Math.Exp(-hours / 12));     // fast start, then flat
        var reach = baseReach * formatBoost * growth;

        decimal R(double factor) => Math.Round((decimal)(reach * factor));
        IReadOnlyDictionary<MetricCode, decimal> values = new Dictionary<MetricCode, decimal>
        {
            [MetricCode.Reach] = R(1),
            [MetricCode.Impressions] = R(1.4),
            [MetricCode.Likes] = R(0.06),
            [MetricCode.Comments] = R(0.008),
            [MetricCode.Shares] = R(0.004),
            [MetricCode.Saves] = R(0.01),
            [MetricCode.LinkClicks] = R(0.015),
            [MetricCode.ProfileVisits] = R(0.02),
            [MetricCode.Follows] = R(0.003)
        };
        return Task.FromResult(values);
    }

    // string.GetHashCode() changes between runs; this does not.
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in value) hash = hash * 31 + c;
            return hash;
        }
    }
}
