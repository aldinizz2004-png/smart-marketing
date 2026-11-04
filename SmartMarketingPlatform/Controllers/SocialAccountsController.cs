using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.DTOs.Social;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Connecting accounts hands the platform the right to publish: Owner/Admin only.
[ApiController]
[Route("api/businesses/{businessId}/social-accounts")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class SocialAccountsController(ISocialAccountService accounts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SocialAccountResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SocialAccountResponse>>> GetAll(Guid businessId, CancellationToken cancellationToken)
        => Ok(await accounts.GetAllAsync(businessId, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<SocialAccountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SocialAccountResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await accounts.GetAsync(businessId, id, cancellationToken));

    // POST -> 201 new account ; 200 when a disconnected account of this business is reconnected
    [HttpPost]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<SocialAccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<SocialAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SocialAccountResponse>> Connect(Guid businessId, ConnectSocialAccountRequest request, CancellationToken cancellationToken)
    {
        var (account, created) = await accounts.ConnectAsync(businessId, request, cancellationToken);
        return created ? CreatedAtAction(nameof(GetById), new { businessId, id = account.Id }, account) : Ok(account);
    }

    // PUT .../credentials -> replace the token (e.g. after it expired)
    [HttpPut("{id}/credentials")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<SocialAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SocialAccountResponse>> UpdateCredentials(Guid businessId, Guid id, UpdateCredentialsRequest request, CancellationToken cancellationToken)
        => Ok(await accounts.UpdateCredentialsAsync(businessId, id, request, cancellationToken));

    // POST .../disconnect -> token deleted, account kept for history
    [HttpPost("{id}/disconnect")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<SocialAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SocialAccountResponse>> Disconnect(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await accounts.DisconnectAsync(businessId, id, cancellationToken));
}
