using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Strategy;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/strategy-runs")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class StrategyRunsController(IStrategyService strategy) : ControllerBase
{
    // POST .../strategy-runs { "campaignId": "...", "windowDays": 7 } -> 201 run with its recommendations
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<StrategyRunResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<StrategyRunResponse>> Start(Guid businessId, StartStrategyRunRequest request, CancellationToken cancellationToken)
    {
        var run = await strategy.StartRunAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = run.Id }, run);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<StrategyRunResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StrategyRunResponse>>> GetAll(Guid businessId, [FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await strategy.GetRunsAsync(businessId, paging, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<StrategyRunResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StrategyRunResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await strategy.GetRunAsync(businessId, id, cancellationToken));
}
