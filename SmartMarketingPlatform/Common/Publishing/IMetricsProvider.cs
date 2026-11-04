using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Publishing;

// Reads the numbers of a published post from its platform (Fake today).
public interface IMetricsProvider
{
    Task<IReadOnlyDictionary<MetricCode, decimal>> GetMetricsAsync(MetricsRequest request, CancellationToken cancellationToken);
}

public sealed record MetricsRequest(string PlatformCode, string ExternalPostId, DateTimeOffset PublishedAt, DateTimeOffset Now, ContentFormat Format);
