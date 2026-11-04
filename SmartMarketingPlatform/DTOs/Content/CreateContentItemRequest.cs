using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Content;

public sealed class CreateContentItemRequest
{
    [Required]
    public Guid? CampaignId { get; init; }

    [Required, MaxLength(30)]
    public string? PlatformCode { get; init; }

    [Required, EnumDataType(typeof(ContentType))]
    public ContentType? ContentType { get; init; }

    public DateTimeOffset? PlannedPublishAt { get; init; }

    [MaxLength(10)]
    public string? VariantLabel { get; init; }                  // "A" / "B" for A/B tests

    [Required]
    public RevisionRequest? FirstRevision { get; init; }
}
