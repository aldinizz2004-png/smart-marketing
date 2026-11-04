using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Campaigns;
using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.Services;

public interface ICampaignService
{
    Task<PagedResult<CampaignResponse>> GetAllAsync(Guid businessId, CampaignListQuery query, CancellationToken cancellationToken);
    Task<CampaignResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<CampaignResponse> CreateAsync(Guid businessId, CampaignRequest request, CancellationToken cancellationToken);
    Task<CampaignResponse> UpdateAsync(Guid businessId, Guid id, CampaignRequest request, CancellationToken cancellationToken);
    Task<CampaignResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken);

    Task<CampaignPlanResponse> GeneratePlanAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CampaignPlanResponse>> GetPlansAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<CampaignPlanResponse> GetPlanAsync(Guid businessId, Guid id, int versionNo, CancellationToken cancellationToken);
}
