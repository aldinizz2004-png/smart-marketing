using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Comments;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// A reply is public and speaks for the business, so it needs the same role as approving a post.
[ApiController]
[Route("api/businesses/{businessId}/comments")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class CommentsController(ICommentService comments) : ControllerBase
{
    // GET .../comments?status=Analyzed&category=Complaint&needsReply=true
    [HttpGet]
    [ProducesResponseType<PagedResult<CommentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CommentResponse>>> GetAll(Guid businessId, [FromQuery] CommentListQuery filter, CancellationToken cancellationToken)
        => Ok(await comments.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await comments.GetAsync(businessId, id, cancellationToken));

    // Pulls the latest comments of the last 30 days of publications (webhooks bring new ones automatically).
    [HttpPost("sync")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CommentSyncResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CommentSyncResponse>> Sync(Guid businessId, CancellationToken cancellationToken)
        => Ok(await comments.SyncAsync(businessId, cancellationToken));

    // The AI classifies the new comments and suggests replies.
    [HttpPost("analyze")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CommentAnalysisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CommentAnalysisResponse>> Analyze(Guid businessId, CancellationToken cancellationToken)
        => Ok(await comments.AnalyzeAsync(businessId, cancellationToken));

    // POST .../comments/{id}/reply  { "text": "..." }  (empty body = send the AI suggestion)
    [HttpPost("{id}/reply")]
    [Authorize(Policy = Policies.CanReview)]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CommentResponse>> Reply(Guid businessId, Guid id, ReplyToCommentRequest request, CancellationToken cancellationToken)
        => Ok(await comments.ReplyAsync(businessId, id, request, cancellationToken));

    [HttpPost("{id}/ignore")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommentResponse>> Ignore(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await comments.IgnoreAsync(businessId, id, cancellationToken));
}
