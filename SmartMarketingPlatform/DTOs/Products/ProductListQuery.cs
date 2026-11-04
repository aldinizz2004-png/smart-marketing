using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;

namespace SmartMarketingPlatform.DTOs.Products;

// GET .../products?search=latte&isActive=true&page=1
public sealed class ProductListQuery : PageQuery
{
    [MaxLength(100)]
    public string? Search { get; init; }

    public bool? IsActive { get; init; }
}
