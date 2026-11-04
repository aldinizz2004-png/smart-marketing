using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Publishing;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class PublishingController(IPublishingService publishing, IPerformanceService performance) : ControllerBase
{
    // POST .../content-items/{id}/schedule { "publishAt": "...", "socialAccountId": "..." } (both optional)
    [HttpPost("content-items/{id}/schedule")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<PublicationJobResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublicationJobResponse>> Schedule(Guid businessId, Guid id, ScheduleRequest? request, CancellationToken cancellationToken)
    {
        var job = await publishing.ScheduleAsync(businessId, id, request ?? new ScheduleRequest(), cancellationToken);
        return CreatedAtAction(nameof(GetJob), new { businessId, jobId = job.Id }, job);
    }

    // GET .../publication-jobs?status=Queued
    [HttpGet("publication-jobs")]
    [ProducesResponseType<PagedResult<PublicationJobResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PublicationJobResponse>>> GetJobs(Guid businessId, [FromQuery] JobListQuery filter, CancellationToken cancellationToken)
        => Ok(await publishing.GetJobsAsync(businessId, filter, cancellationToken));

    [HttpGet("publication-jobs/{jobId}")]
    [ProducesResponseType<PublicationJobResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicationJobResponse>> GetJob(Guid businessId, Guid jobId, CancellationToken cancellationToken)
        => Ok(await publishing.GetJobAsync(businessId, jobId, cancellationToken));

    // POST .../publication-jobs/{jobId}/cancel -> only while waiting (Queued / Retry)
    [HttpPost("publication-jobs/{jobId}/cancel")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<PublicationJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublicationJobResponse>> CancelJob(Guid businessId, Guid jobId, CancellationToken cancellationToken)
        => Ok(await publishing.CancelJobAsync(businessId, jobId, cancellationToken));

    // GET .../publications?page=1 -> what went out, with the latest numbers
    [HttpGet("publications")]
    [ProducesResponseType<PagedResult<PublicationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PublicationResponse>>> GetPublications(Guid businessId, [FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await performance.GetPublicationsAsync(businessId, paging, cancellationToken));

    // GET .../publications/{id}/metrics -> every snapshot (the curve over time)
    [HttpGet("publications/{publicationId}/metrics")]
    [ProducesResponseType<IReadOnlyList<MetricPoint>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MetricPoint>>> GetMetrics(Guid businessId, Guid publicationId, CancellationToken cancellationToken)
        => Ok(await performance.GetMetricHistoryAsync(businessId, publicationId, cancellationToken));

    // POST .../publications/{id}/metrics/refresh -> collect a snapshot now
    [HttpPost("publications/{publicationId}/metrics/refresh")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<IReadOnlyList<MetricPoint>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MetricPoint>>> RefreshMetrics(Guid businessId, Guid publicationId, CancellationToken cancellationToken)
        => Ok(await performance.RefreshMetricsAsync(businessId, publicationId, cancellationToken));

    // GET .../performance?from=...&to=... -> totals, by platform / format / hook, top posts
    [HttpGet("performance")]
    [ProducesResponseType<PerformanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PerformanceResponse>> GetPerformance(Guid businessId, [FromQuery] PerformanceQuery query, CancellationToken cancellationToken)
        => Ok(await performance.GetPerformanceAsync(businessId, query, cancellationToken));
}
