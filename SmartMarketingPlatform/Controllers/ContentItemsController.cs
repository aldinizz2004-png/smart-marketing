using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Content;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/content-items")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class ContentItemsController(IContentService content) : ControllerBase
{
    // GET .../content-items?campaignId=...&status=InReview&platform=instagram  (latest revision only)
    [HttpGet]
    [ProducesResponseType<PagedResult<ContentItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ContentItemResponse>>> GetAll(Guid businessId, [FromQuery] ContentListQuery filter, CancellationToken cancellationToken)
        => Ok(await content.GetAllAsync(businessId, filter, cancellationToken));

    // GET .../content-items/{id}  (every revision with its reviews)
    [HttpGet("{id}")]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContentItemResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await content.GetAsync(businessId, id, cancellationToken));

    // POST -> new Draft with revision 1 (written by you, or by the AI with firstRevision.generateWithAi = true)
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ContentItemResponse>> Create(Guid businessId, CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await content.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = item.Id }, item);
    }

    // POST .../revisions -> a new version of the text (an approved item goes back to Draft)
    [HttpPost("{id}/revisions")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ContentItemResponse>> AddRevision(Guid businessId, Guid id, RevisionRequest request, CancellationToken cancellationToken)
    {
        var item = await content.AddRevisionAsync(businessId, id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id }, item);
    }

    // PATCH .../status { "status": "InReview" } (submit) | "Draft" (withdraw) | "Canceled"
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentItemResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await content.ChangeStatusAsync(businessId, id, request, cancellationToken));

    // POST .../reviews { "decision": "Approved" | "ChangesRequested" | "Rejected", "feedback": "..." }
    // A review is a new resource (who decided what, on which revision), hence POST + 201.
    [HttpPost("{id}/reviews")]
    [Authorize(Policy = Policies.CanReview)]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentItemResponse>> Review(Guid businessId, Guid id, ReviewRequest request, CancellationToken cancellationToken)
    {
        var item = await content.ReviewAsync(businessId, id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id }, item);
    }
}
