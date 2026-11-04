using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.DTOs.Brand;
using SmartMarketingPlatform.Enums;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class BrandController(IBrandService brand) : ControllerBase
{
    [HttpGet("brand-profile")]
    [ProducesResponseType<BrandProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandProfileResponse>> GetProfile(Guid businessId, CancellationToken cancellationToken)
        => Ok(await brand.GetProfileAsync(businessId, cancellationToken));

    // PUT creates the profile the first time (201) and replaces it afterwards (200): one profile per business.
    [HttpPut("brand-profile")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<BrandProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<BrandProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandProfileResponse>> PutProfile(Guid businessId, BrandProfileRequest request, CancellationToken cancellationToken)
    {
        var (profile, created) = await brand.UpsertProfileAsync(businessId, request, cancellationToken);
        return created ? CreatedAtAction(nameof(GetProfile), new { businessId }, profile) : Ok(profile);
    }

    // GET .../brand-terms?rule=Prohibited
    [HttpGet("brand-terms")]
    [ProducesResponseType<IReadOnlyList<BrandTermResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BrandTermResponse>>> GetTerms(Guid businessId, [FromQuery] BrandTermRule? rule, CancellationToken cancellationToken)
        => Ok(await brand.GetTermsAsync(businessId, rule, cancellationToken));

    [HttpPost("brand-terms")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<BrandTermResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandTermResponse>> AddTerm(Guid businessId, BrandTermRequest request, CancellationToken cancellationToken)
    {
        var term = await brand.AddTermAsync(businessId, request, cancellationToken);
        return Created($"/api/businesses/{businessId}/brand-terms", term);
    }

    [HttpDelete("brand-terms/{termId}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTerm(Guid businessId, Guid termId, CancellationToken cancellationToken)
    {
        await brand.DeleteTermAsync(businessId, termId, cancellationToken);
        return NoContent();
    }
}
