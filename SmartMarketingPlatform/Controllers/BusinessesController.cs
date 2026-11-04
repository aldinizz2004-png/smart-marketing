using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Businesses;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Every business endpoint needs an active subscription (402 otherwise).
[ApiController]
[Route("api/businesses")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
public class BusinessesController(IBusinessService businesses) : ControllerBase
{
    // GET /api/businesses?status=Active&search=cafe&page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType<PagedResult<BusinessResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BusinessResponse>>> GetAll([FromQuery] BusinessListQuery filter, CancellationToken cancellationToken)
        => Ok(await businesses.GetAllAsync(filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<BusinessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await businesses.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<BusinessResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessResponse>> Create(BusinessRequest request, CancellationToken cancellationToken)
    {
        var business = await businesses.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = business.Id }, business);
    }

    // PUT = full replace of the editable fields.
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<BusinessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BusinessResponse>> Update(Guid id, BusinessRequest request, CancellationToken cancellationToken)
        => Ok(await businesses.UpdateAsync(id, request, cancellationToken));

    // PATCH /api/businesses/{id}/status  { "status": "Paused" }  (Closed is final: no DELETE, history is kept)
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<BusinessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BusinessResponse>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await businesses.ChangeStatusAsync(id, request, cancellationToken));
}
