using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.RateLimiting;
using SmartMarketingPlatform.DTOs.Auth;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Anonymous per action, not per class: [AllowAnonymous] on the class would also open logout-all.
// Send "X-Auth-Transport: cookie" to keep the refresh token in an httpOnly cookie instead of the body.
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    // POST /api/auth/register -> 201 + Location: /api/me
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await auth.RegisterAsync(request, cancellationToken);
        return Created("/api/me", Deliver(response));
    }

    // POST /api/auth/login -> 200 tokens ; 401 wrong email/password ; 403 no active tenant
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(Deliver(await auth.LoginAsync(request, cancellationToken)));

    // POST /api/auth/refresh { "refreshToken": "..." } (or the cookie) -> a new pair; the old refresh token stops working
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = TokenFrom(request);
        if (token is null)
            return MissingToken();

        var response = await auth.RefreshAsync(new RefreshTokenRequest { RefreshToken = token }, cancellationToken);
        return Ok(Deliver(response));
    }

    // POST /api/auth/logout { "refreshToken": "..." } (or the cookie) -> ends this session (the access token simply expires)
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var usesCookie = RefreshCookie.IsRequested(Request);
        var token = TokenFrom(request);
        if (usesCookie)
            RefreshCookie.Clear(HttpContext);
        if (token is null)
            return usesCookie ? NoContent() : MissingToken();

        await auth.LogoutAsync(new RefreshTokenRequest { RefreshToken = token }, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/logout-all (with the access token) -> ends every session of the user, e.g. after a lost phone
    [HttpPost("logout-all")]
    [ProducesResponseType<LogoutAllResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LogoutAllResponse>> LogoutAll(CancellationToken cancellationToken)
    {
        var ended = await auth.LogoutEverywhereAsync(currentUser.UserId, cancellationToken);
        if (RefreshCookie.IsRequested(Request))
            RefreshCookie.Clear(HttpContext);
        return Ok(new LogoutAllResponse(ended));
    }

    private AuthResponse Deliver(AuthResponse response)
    {
        if (!RefreshCookie.IsRequested(Request) || response.RefreshToken is null)
            return response;

        RefreshCookie.Write(HttpContext, response.RefreshToken, response.RefreshTokenExpiresAt);
        return response with { RefreshToken = null };
    }

    private string? TokenFrom(RefreshTokenRequest? request) =>
        string.IsNullOrWhiteSpace(request?.RefreshToken) ? RefreshCookie.Read(Request) : request.RefreshToken;

    private ActionResult MissingToken()
    {
        ModelState.AddModelError(nameof(RefreshTokenRequest.RefreshToken),
            $"Send the refresh token in the body, or send the cookie with the {RefreshCookie.TransportHeader}: {RefreshCookie.CookieTransport} header.");
        return ValidationProblem();
    }
}
