using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.DTOs.Account;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/me")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class MeController(IAccountService account) : ControllerBase
{
    // GET /api/me -> profile + all tenants of the user
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
        => Ok(await account.GetMeAsync(cancellationToken));

    // PATCH /api/me -> partial update (only the fields sent)
    [HttpPatch]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await account.UpdateMeAsync(request, cancellationToken));
}
