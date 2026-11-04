using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Reports;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Weekly reports are made by a background worker; these endpoints list them and make one on demand.
[ApiController]
[Route("api/businesses/{businessId}/reports")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ReportSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportSummaryResponse>>> GetAll(Guid businessId, [FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await reports.GetAllAsync(businessId, paging, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await reports.GetAsync(businessId, id, cancellationToken));

    // The report as the owner receives it by email.
    [HttpGet("{id}/html")]
    [Produces("text/html")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ContentResult> GetHtml(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Content(await reports.GetHtmlAsync(businessId, id, cancellationToken), "text/html; charset=utf-8");

    // POST .../reports  { "days": 7, "send": true }
    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ReportResponse>> Generate(Guid businessId, GenerateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await reports.GenerateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = report.Id }, report);
    }

    [HttpPost("{id}/send")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ReportResponse>> Send(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await reports.SendAsync(businessId, id, cancellationToken));
}
