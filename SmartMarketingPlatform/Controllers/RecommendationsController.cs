using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Content;
using SmartMarketingPlatform.DTOs.Strategy;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/recommendations")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class RecommendationsController(IStrategyService strategy, IContentService content) : ControllerBase
{
    // GET .../recommendations?status=Pending  -> the owner's "inbox"
    [HttpGet]
    [ProducesResponseType<PagedResult<RecommendationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RecommendationResponse>>> GetAll(Guid businessId, [FromQuery] RecommendationListQuery filter, CancellationToken cancellationToken)
        => Ok(await strategy.GetRecommendationsAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<RecommendationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendationResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await strategy.GetRecommendationAsync(businessId, id, cancellationToken));

    // PATCH .../recommendations/{id}/status { "status": "Accepted" | "Rejected" }
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<RecommendationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RecommendationResponse>> Decide(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await strategy.DecideAsync(businessId, id, request, cancellationToken));

    // POST .../recommendations/{id}/apply -> an accepted NewContent recommendation becomes a content item
    // written by the AI (201, Location = the new content item). The recommendation becomes Applied.
    [HttpPost("{id}/apply")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ContentItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ContentItemResponse>> Apply(Guid businessId, Guid id, ApplyRecommendationRequest? request, CancellationToken cancellationToken)
    {
        var item = await content.ApplyRecommendationAsync(businessId, id, request ?? new ApplyRecommendationRequest(), cancellationToken);
        return Created($"/api/businesses/{businessId}/content-items/{item.Id}", item);
    }
}
