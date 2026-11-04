using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Publishing;

namespace SmartMarketingPlatform.Services;

public interface IPerformanceService
{
    Task<PagedResult<PublicationResponse>> GetPublicationsAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<MetricPoint>> GetMetricHistoryAsync(Guid businessId, Guid publicationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MetricPoint>> RefreshMetricsAsync(Guid businessId, Guid publicationId, CancellationToken cancellationToken);
    Task<PerformanceResponse> GetPerformanceAsync(Guid businessId, PerformanceQuery query, CancellationToken cancellationToken);
}
