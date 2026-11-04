using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Subscriptions;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/tenant/subscriptions")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class SubscriptionsController(ISubscriptionService subscriptions) : ControllerBase
{
    // GET /api/tenant/subscriptions?page=1 -> full history, newest first
    [HttpGet]
    [ProducesResponseType<PagedResult<SubscriptionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SubscriptionResponse>>> GetHistory([FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await subscriptions.GetHistoryAsync(paging, cancellationToken));

    // GET /api/tenant/subscriptions/current -> the period that grants access now, or 404
    [HttpGet("current")]
    [ProducesResponseType<SubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var current = await subscriptions.FindCurrentAsync(cancellationToken);
        return current is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: "The tenant has no active subscription.")
            : Ok(current);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<SubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await subscriptions.GetAsync(id, cancellationToken));

    // POST /api/tenant/subscriptions -> 201 Pending (a renewal if a paid period is running)
    [HttpPost]
    [Authorize(Policy = Policies.TenantOwner)]
    [ProducesResponseType<SubscriptionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionResponse>> Create(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
    }

    // POST /api/tenant/subscriptions/{id}/activate -> payment confirmed, Pending -> Active
    [HttpPost("{id}/activate")]
    [Authorize(Policy = Policies.TenantOwner)]
    [ProducesResponseType<SubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionResponse>> Activate(Guid id, ActivateSubscriptionRequest request, CancellationToken cancellationToken)
        => Ok(await subscriptions.ActivateAsync(id, request, cancellationToken));

    // POST /api/tenant/subscriptions/{id}/cancel -> Pending -> Canceled
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = Policies.TenantOwner)]
    [ProducesResponseType<SubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionResponse>> Cancel(Guid id, CancellationToken cancellationToken)
        => Ok(await subscriptions.CancelAsync(id, cancellationToken));
}
