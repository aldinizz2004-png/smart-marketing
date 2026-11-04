using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Strategy;

namespace SmartMarketingPlatform.Services;

public interface IStrategyService
{
    Task<StrategyRunResponse> StartRunAsync(Guid businessId, StartStrategyRunRequest request, CancellationToken cancellationToken);
    Task<PagedResult<StrategyRunResponse>> GetRunsAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken);
    Task<StrategyRunResponse> GetRunAsync(Guid businessId, Guid runId, CancellationToken cancellationToken);

    Task<PagedResult<RecommendationResponse>> GetRecommendationsAsync(Guid businessId, RecommendationListQuery query, CancellationToken cancellationToken);
    Task<RecommendationResponse> GetRecommendationAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<RecommendationResponse> DecideAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);
}
