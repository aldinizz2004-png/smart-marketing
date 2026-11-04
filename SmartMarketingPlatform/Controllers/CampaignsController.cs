using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Campaigns;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/campaigns")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class CampaignsController(ICampaignService campaigns) : ControllerBase
{
    // GET .../campaigns?status=Active&objective=Orders&page=1
    [HttpGet]
    [ProducesResponseType<PagedResult<CampaignResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CampaignResponse>>> GetAll(Guid businessId, [FromQuery] CampaignListQuery filter, CancellationToken cancellationToken)
        => Ok(await campaigns.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<CampaignResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CampaignResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await campaigns.GetAsync(businessId, id, cancellationToken));

    // New campaigns start as Draft.
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CampaignResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignResponse>> Create(Guid businessId, CampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = campaign.Id }, campaign);
    }

    // Draft or Ready only; editing a Ready campaign sends it back to Draft (its plan is outdated).
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CampaignResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CampaignResponse>> Update(Guid businessId, Guid id, CampaignRequest request, CancellationToken cancellationToken)
        => Ok(await campaigns.UpdateAsync(businessId, id, request, cancellationToken));

    // PATCH .../status { "status": "Active" }  (Active needs Owner/Admin; no DELETE: cancel instead)
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CampaignResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CampaignResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await campaigns.ChangeStatusAsync(businessId, id, request, cancellationToken));

    // POST .../plans -> the AI writes a new plan version (201). AI failure -> 502.
    [HttpPost("{id}/plans")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CampaignPlanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CampaignPlanResponse>> GeneratePlan(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        var plan = await campaigns.GeneratePlanAsync(businessId, id, cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { businessId, id, versionNo = plan.VersionNo }, plan);
    }

    // All versions, newest first.
    [HttpGet("{id}/plans")]
    [ProducesResponseType<IReadOnlyList<CampaignPlanResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampaignPlanResponse>>> GetPlans(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await campaigns.GetPlansAsync(businessId, id, cancellationToken));

    [HttpGet("{id}/plans/{versionNo:int}")]
    [ProducesResponseType<CampaignPlanResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CampaignPlanResponse>> GetPlan(Guid businessId, Guid id, int versionNo, CancellationToken cancellationToken)
        => Ok(await campaigns.GetPlanAsync(businessId, id, versionNo, cancellationToken));
}
