using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Competitors;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Accounts the owner learns from: competitors, professionals in the field, inspirations.
[ApiController]
[Route("api/businesses/{businessId}/tracked-sources")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class TrackedSourcesController(ICompetitorService competitors) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TrackedSourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TrackedSourceResponse>>> GetAll(Guid businessId, [FromQuery] TrackedSourceListQuery filter, CancellationToken cancellationToken)
        => Ok(await competitors.GetSourcesAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<TrackedSourceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackedSourceResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await competitors.GetSourceAsync(businessId, id, cancellationToken));

    // The account is scanned once right away; afterwards the background worker refreshes it daily.
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<TrackedSourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackedSourceResponse>> Create(Guid businessId, TrackedSourceRequest request, CancellationToken cancellationToken)
    {
        var created = await competitors.AddSourceAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = created.Id }, created);
    }

    // PATCH .../{id}/status { "status": "Paused" } or { "status": "Active" }
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<TrackedSourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TrackedSourceResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await competitors.ChangeStatusAsync(businessId, id, request, cancellationToken));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await competitors.DeleteSourceAsync(businessId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/scan")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ScanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ScanResponse>> Scan(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await competitors.ScanAsync(businessId, id, cancellationToken));

    [HttpGet("{id}/posts")]
    [ProducesResponseType<PagedResult<SourcePostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SourcePostResponse>>> GetPosts(Guid businessId, Guid id, [FromQuery] PageQuery query, CancellationToken cancellationToken)
        => Ok(await competitors.GetPostsAsync(businessId, id, query, cancellationToken));
}
