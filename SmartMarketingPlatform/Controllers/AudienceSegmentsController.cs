using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Audience;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/audience-segments")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class AudienceSegmentsController(IAudienceSegmentService segments) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AudienceSegmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AudienceSegmentResponse>>> GetAll(Guid businessId, [FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await segments.GetAllAsync(businessId, paging, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<AudienceSegmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceSegmentResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await segments.GetAsync(businessId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<AudienceSegmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AudienceSegmentResponse>> Create(Guid businessId, AudienceSegmentRequest request, CancellationToken cancellationToken)
    {
        var segment = await segments.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = segment.Id }, segment);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<AudienceSegmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AudienceSegmentResponse>> Update(Guid businessId, Guid id, AudienceSegmentRequest request, CancellationToken cancellationToken)
        => Ok(await segments.UpdateAsync(businessId, id, request, cancellationToken));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await segments.DeleteAsync(businessId, id, cancellationToken);
        return NoContent();
    }
}
