using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.RateLimiting;
using SmartMarketingPlatform.DTOs.Competitors;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// What was learned from the tracked sources. Strategy runs and AI posts use the latest insights.
[ApiController]
[Route("api/businesses/{businessId}/competitor-insights")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class CompetitorInsightsController(ICompetitorService competitors) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CompetitorInsightResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompetitorInsightResponse>>> GetLatest(Guid businessId, CancellationToken cancellationToken)
        => Ok(await competitors.GetLatestInsightsAsync(businessId, cancellationToken));

    [HttpPost("analyze")]

    [EnableRateLimiting(RateLimitPolicies.Expensive)]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<InsightAnalysisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<InsightAnalysisResponse>> Analyze(Guid businessId, CancellationToken cancellationToken)
        => Ok(await competitors.AnalyzeAsync(businessId, cancellationToken));
}
