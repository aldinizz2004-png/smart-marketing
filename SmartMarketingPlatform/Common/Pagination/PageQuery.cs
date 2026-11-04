using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.Common.Pagination;

// Query parameters for every list endpoint: ?page=1&pageSize=20
// Out-of-range values return 400 automatically ([ApiController] validation).
// List endpoints with filters inherit from it (e.g. ProductListQuery adds ?search=).
public class PageQuery
{
    public const int MaxPageSize = 100;

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;
}
