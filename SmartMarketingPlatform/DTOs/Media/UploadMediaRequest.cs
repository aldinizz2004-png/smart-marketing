using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Media;

// multipart/form-data: file + kind (+ productId when it is a product photo).
public sealed class UploadMediaRequest
{
    [Required]
    public IFormFile? File { get; init; }

    [Required, EnumDataType(typeof(MediaKind))]
    public MediaKind? Kind { get; init; }

    public Guid? ProductId { get; init; }
}
