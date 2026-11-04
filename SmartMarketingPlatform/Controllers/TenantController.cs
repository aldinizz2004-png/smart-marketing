using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.DTOs.Tenants;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// "api/tenant" (singular): always the tenant of the access token, never an id in the URL.
[ApiController]
[Route("api/tenant")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class TenantController(ITenantService tenants) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TenantResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantResponse>> Get(CancellationToken cancellationToken)
        => Ok(await tenants.GetCurrentAsync(cancellationToken));

    [HttpPatch]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<TenantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TenantResponse>> Update(UpdateTenantRequest request, CancellationToken cancellationToken)
        => Ok(await tenants.UpdateAsync(request, cancellationToken));
}
