using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Knowledge;

public sealed class KnowledgeListQuery : PageQuery
{
    public KnowledgeSourceType? Type { get; init; }

    [MaxLength(30)]
    public string? Status { get; init; }
}
