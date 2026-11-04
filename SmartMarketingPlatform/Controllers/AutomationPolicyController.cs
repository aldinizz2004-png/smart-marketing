using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.DTOs.Automation;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// The owner's limits on automation. Everyone in the tenant can read them; only Owner/Admin can change them.
[ApiController]
[Route("api/businesses/{businessId}/automation-policy")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class AutomationPolicyController(IAutomationPolicyService policies) : ControllerBase
{
    // Never saved -> 200 with safe defaults (automation off) and isConfigured=false.
    [HttpGet]
    [ProducesResponseType<AutomationPolicyResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AutomationPolicyResponse>> Get(Guid businessId, CancellationToken cancellationToken)
        => Ok(await policies.GetAsync(businessId, cancellationToken));

    // PUT -> 201 the first time, 200 afterwards.
    [HttpPut]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<AutomationPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<AutomationPolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AutomationPolicyResponse>> Put(Guid businessId, AutomationPolicyRequest request, CancellationToken cancellationToken)
    {
        var (policy, created) = await policies.UpsertAsync(businessId, request, cancellationToken);
        return created ? CreatedAtAction(nameof(Get), new { businessId }, policy) : Ok(policy);
    }
}
