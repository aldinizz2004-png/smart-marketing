using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Media;

// GET .../media?kind=ProductPhoto&productId=...&page=1
public sealed class MediaListQuery : PageQuery
{
    public MediaKind? Kind { get; init; }
    public Guid? ProductId { get; init; }
}
