using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Strategy;

// GET .../recommendations?status=Pending&campaignId=...
public sealed class RecommendationListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    public Guid? CampaignId { get; init; }
}
