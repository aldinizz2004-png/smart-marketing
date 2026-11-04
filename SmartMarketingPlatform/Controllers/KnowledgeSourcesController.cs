using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Knowledge;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/knowledge-sources")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class KnowledgeSourcesController(IKnowledgeSourceService knowledge) : ControllerBase
{
    // GET .../knowledge-sources?type=Menu&status=Processed  (text preview only)
    [HttpGet]
    [ProducesResponseType<PagedResult<KnowledgeSourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<KnowledgeSourceResponse>>> GetAll(Guid businessId, [FromQuery] KnowledgeListQuery filter, CancellationToken cancellationToken)
        => Ok(await knowledge.GetAllAsync(businessId, filter, cancellationToken));

    // GET .../knowledge-sources/{id}  (full extracted text)
    [HttpGet("{id}")]
    [ProducesResponseType<KnowledgeSourceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeSourceResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await knowledge.GetAsync(businessId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<KnowledgeSourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KnowledgeSourceResponse>> Create(Guid businessId, KnowledgeSourceRequest request, CancellationToken cancellationToken)
    {
        var source = await knowledge.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = source.Id }, source);
    }

    // PATCH .../knowledge-sources/{id}/status  { "status": "Disabled" } or { "status": "Pending" } (re-process)
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<KnowledgeSourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KnowledgeSourceResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await knowledge.ChangeStatusAsync(businessId, id, request, cancellationToken));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await knowledge.DeleteAsync(businessId, id, cancellationToken);
        return NoContent();
    }
}
