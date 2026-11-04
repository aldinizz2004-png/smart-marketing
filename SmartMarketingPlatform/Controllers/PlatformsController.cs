using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Localization;
using SmartMarketingPlatform.DTOs.Lookups;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/platforms")]
[AllowAnonymous]   // reference data, public
public class PlatformsController(ILookupService lookups, PlatformDetector detector) : ControllerBase
{
    // GET /api/platforms?lang=ar
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "Accept-Language")]
    [ProducesResponseType<IReadOnlyList<PlatformResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlatformResponse>>> GetAll(
        [FromQuery] string? lang, CancellationToken cancellationToken)
    {
        var language = RequestLanguage.Resolve(HttpContext, lang);
        return Ok(await lookups.GetPlatformsAsync(language, cancellationToken));
    }

    // GET /api/platforms/instagram
    [HttpGet("{code}")]
    [ResponseCache(Duration = 300, VaryByHeader = "Accept-Language")]
    [ProducesResponseType<PlatformResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlatformResponse>> GetByCode(
        string code, [FromQuery] string? lang, CancellationToken cancellationToken)
    {
        var language = RequestLanguage.Resolve(HttpContext, lang);
        return Ok(await lookups.GetPlatformAsync(code, language, cancellationToken));
    }

    // GET /api/platforms/detect?url=https://www.instagram.com/rival_cafe/
    // The literal segment "detect" takes precedence over {code}.
    [HttpGet("detect")]
    [ProducesResponseType<DetectedUrl>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DetectedUrl>> Detect(
        [FromQuery, Required, MaxLength(2048)] string url, CancellationToken cancellationToken)
    {
        return Ok(await detector.DetectAsync(url, cancellationToken));
    }
}
