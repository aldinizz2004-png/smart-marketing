using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.DTOs.Usage;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// AI cost and debugging: Owner/Admin only. Usage stays readable without a subscription
// (an owner whose plan expired must still see what was spent).
[ApiController]
[Authorize(Policy = Policies.TenantAdmin)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class AiUsageController(IAiUsageService usage) : ControllerBase
{
    // GET /api/tenant/ai-usage?from=2026-10-01&to=2026-11-01
    [HttpGet("api/tenant/ai-usage")]
    [ProducesResponseType<AiUsageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiUsageResponse>> GetUsage([FromQuery] AiUsageQuery query, CancellationToken cancellationToken)
        => Ok(await usage.GetUsageAsync(query, cancellationToken));

    // GET /api/businesses/{businessId}/ai-executions/{id} -> what was sent, what came back, cost, error
    [HttpGet("api/businesses/{businessId}/ai-executions/{id}")]
    [ProducesResponseType<AiExecutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiExecutionResponse>> GetExecution(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await usage.GetExecutionAsync(businessId, id, cancellationToken));
}
