using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Competitors;

namespace SmartMarketingPlatform.Services;

public interface ICompetitorService
{
    Task<PagedResult<TrackedSourceResponse>> GetSourcesAsync(Guid businessId, TrackedSourceListQuery query, CancellationToken cancellationToken);
    Task<TrackedSourceResponse> GetSourceAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<TrackedSourceResponse> AddSourceAsync(Guid businessId, TrackedSourceRequest request, CancellationToken cancellationToken);
    Task<TrackedSourceResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
    Task DeleteSourceAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<ScanResponse> ScanAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<PagedResult<SourcePostResponse>> GetPostsAsync(Guid businessId, Guid id, PageQuery query, CancellationToken cancellationToken);

    Task<InsightAnalysisResponse> AnalyzeAsync(Guid businessId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompetitorInsightResponse>> GetLatestInsightsAsync(Guid businessId, CancellationToken cancellationToken);

    // Background: re-scan sources that are due, then refresh the insights of businesses with new posts.
    Task<(int Scanned, int Analyzed)> RunScheduledAsync(CancellationToken cancellationToken);
}
