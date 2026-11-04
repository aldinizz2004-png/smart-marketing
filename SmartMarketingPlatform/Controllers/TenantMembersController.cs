using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Tenants;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/tenant/members")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class TenantMembersController(ITenantService tenants) : ControllerBase
{
    // GET /api/tenant/members?page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType<PagedResult<MemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MemberResponse>>> GetAll([FromQuery] PageQuery paging, CancellationToken cancellationToken)
        => Ok(await tenants.GetMembersAsync(paging, cancellationToken));

    [HttpGet("{userId}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetById(Guid userId, CancellationToken cancellationToken)
        => Ok(await tenants.GetMemberAsync(userId, cancellationToken));

    // POST /api/tenant/members -> 201 + Location: /api/tenant/members/{userId}
    [HttpPost]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> Add(AddMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await tenants.AddMemberAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { userId = member.UserId }, member);
    }

    // PATCH /api/tenant/members/{userId} -> change role and/or enable/disable
    [HttpPatch("{userId}")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> Update(Guid userId, UpdateMemberRequest request, CancellationToken cancellationToken)
        => Ok(await tenants.UpdateMemberAsync(userId, request, cancellationToken));
}
