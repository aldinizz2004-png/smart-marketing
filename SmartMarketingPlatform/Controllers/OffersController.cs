using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Offers;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/offers")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class OffersController(IOfferService offers) : ControllerBase
{
    // GET .../offers?status=Active&page=1
    [HttpGet]
    [ProducesResponseType<PagedResult<OfferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OfferResponse>>> GetAll(Guid businessId, [FromQuery] OfferListQuery filter, CancellationToken cancellationToken)
        => Ok(await offers.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OfferResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await offers.GetAsync(businessId, id, cancellationToken));

    // New offers start as Draft.
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OfferResponse>> Create(Guid businessId, OfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await offers.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = offer.Id }, offer);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferResponse>> Update(Guid businessId, Guid id, OfferRequest request, CancellationToken cancellationToken)
        => Ok(await offers.UpdateAsync(businessId, id, request, cancellationToken));

    // PATCH .../offers/{id}/status  { "status": "Active" }
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await offers.ChangeStatusAsync(businessId, id, request, cancellationToken));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await offers.DeleteAsync(businessId, id, cancellationToken);
        return NoContent();
    }
}
