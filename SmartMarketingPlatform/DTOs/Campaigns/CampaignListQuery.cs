using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Campaigns;

// GET .../campaigns?status=Active&objective=Orders&page=1
public sealed class CampaignListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    public CampaignObjective? Objective { get; init; }
}
