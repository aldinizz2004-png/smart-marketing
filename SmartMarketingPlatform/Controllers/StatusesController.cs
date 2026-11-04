using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Localization;
using SmartMarketingPlatform.DTOs.Lookups;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/statuses")]
[AllowAnonymous]   // reference data, public
public class StatusesController(ILookupService lookups) : ControllerBase
{
    // GET /api/statuses/Campaign?lang=ar   (or header Accept-Language: ar)
    // Statuses change only with a deployment, so clients may cache them for 5 minutes.
    [HttpGet("{category}")]
    [ResponseCache(Duration = 300, VaryByHeader = "Accept-Language")]
    [ProducesResponseType<IReadOnlyList<StatusResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StatusResponse>>> GetByCategory(
        string category, [FromQuery] string? lang, CancellationToken cancellationToken)
    {
        var language = RequestLanguage.Resolve(HttpContext, lang);
        return Ok(await lookups.GetStatusesAsync(category, language, cancellationToken));
    }
}
