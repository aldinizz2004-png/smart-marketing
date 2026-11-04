using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Media;

namespace SmartMarketingPlatform.Services;

public interface IMediaService
{
    Task<PagedResult<MediaAssetResponse>> GetAllAsync(Guid businessId, MediaListQuery query, CancellationToken cancellationToken);
    Task<MediaAssetResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<MediaAssetResponse> UploadAsync(Guid businessId, UploadMediaRequest request, CancellationToken cancellationToken);
    Task<MediaContent> OpenAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}

public sealed record MediaContent(Stream Content, string MimeType, string? Sha256, DateTimeOffset CreatedAt);
