using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Offers;

// GET .../offers?status=Active&page=1
public sealed class OfferListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }
}
