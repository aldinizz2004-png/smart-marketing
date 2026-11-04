using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Media;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/media")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class MediaController(IMediaService media) : ControllerBase
{
    // GET .../media?kind=ProductPhoto&productId=...&page=1
    [HttpGet]
    [ProducesResponseType<PagedResult<MediaAssetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MediaAssetResponse>>> GetAll(Guid businessId, [FromQuery] MediaListQuery filter, CancellationToken cancellationToken)
        => Ok(await media.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<MediaAssetResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaAssetResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await media.GetAsync(businessId, id, cancellationToken));

    // POST multipart/form-data: file, kind, productId?
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaService.MaxUploadBytes + 1024 * 1024)]   // + room for the multipart envelope
    [RequestFormLimits(MultipartBodyLengthLimit = MediaService.MaxUploadBytes + 1024 * 1024)]
    [ProducesResponseType<MediaAssetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<MediaAssetResponse>> Upload(Guid businessId, [FromForm] UploadMediaRequest request, CancellationToken cancellationToken)
    {
        var asset = await media.UploadAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = asset.Id }, asset);
    }

    // GET .../media/{id}/content -> the bytes.
    // ETag = SHA-256 of the file: a client that sends If-None-Match with it gets 304 Not Modified (no body).
    [HttpGet("{id}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> Download(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var file = await media.OpenAsync(businessId, id, cancellationToken);
        Response.Headers.CacheControl = "private, max-age=86400";   // private: the content needs a token
        var etag = file.Sha256 is null ? null : new EntityTagHeaderValue($"\"{file.Sha256}\"");
        return File(file.Content, file.MimeType, file.CreatedAt, etag!, enableRangeProcessing: true);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await media.DeleteAsync(businessId, id, cancellationToken);
        return NoContent();
    }
}
