using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.RateLimiting;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Conversations;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// The WhatsApp inbox. Sending speaks for the business, so it needs the same role as approving a post.
[ApiController]
[Route("api/businesses/{businessId}/conversations")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class ConversationsController(IConversationService conversations) : ControllerBase
{
    // GET .../conversations?status=WaitingForOwner
    [HttpGet]
    [ProducesResponseType<PagedResult<ConversationSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ConversationSummaryResponse>>> GetAll(Guid businessId, [FromQuery] ConversationListQuery filter, CancellationToken cancellationToken)
        => Ok(await conversations.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await conversations.GetAsync(businessId, id, cancellationToken));

    // POST .../messages { "text": "..." } | {} (send the AI suggestion) | { "templateName": "order_update", "languageCode": "ar" }
    [HttpPost("{id}/messages")]
    [Authorize(Policy = Policies.CanReview)]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConversationResponse>> Send(Guid businessId, Guid id, SendMessageRequest request, CancellationToken cancellationToken)
        => Ok(await conversations.SendAsync(businessId, id, request, cancellationToken));

    // A fresh AI suggestion for the current conversation (never sent automatically).
    [HttpPost("{id}/suggest")]
    [EnableRateLimiting(RateLimitPolicies.Expensive)]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConversationResponse>> Suggest(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await conversations.SuggestAsync(businessId, id, cancellationToken));

    // PATCH .../status { "status": "Open" } hands it back to the AI; "Closed" ends it.
    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ConversationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConversationResponse>> ChangeStatus(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await conversations.ChangeStatusAsync(businessId, id, request, cancellationToken));
}
