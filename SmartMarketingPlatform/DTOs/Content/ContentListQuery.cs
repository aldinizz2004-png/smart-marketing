using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Content;

// GET .../content-items?campaignId=...&status=InReview&platform=instagram
public sealed class ContentListQuery : PageQuery
{
    public Guid? CampaignId { get; init; }

    [MaxLength(30)]
    public string? Status { get; init; }

    [MaxLength(30)]
    public string? Platform { get; init; }
}
