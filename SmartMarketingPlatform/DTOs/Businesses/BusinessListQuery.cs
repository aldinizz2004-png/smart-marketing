using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Businesses;

// GET /api/businesses?status=Active&search=cafe&page=1&pageSize=20
public sealed class BusinessListQuery : PageQuery
{
    [MaxLength(30)]
    public string? Status { get; init; }

    [MaxLength(100)]
    public string? Search { get; init; }
}
