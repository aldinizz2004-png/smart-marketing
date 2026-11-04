using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.DTOs.Social;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Called by Postproxy and by the owner's browser, never with our JWT.
[ApiController]
[Route("api/integrations/postproxy")]
[AllowAnonymous]
public class PostproxyIntegrationController(
    ISocialConnectionService connections,
    IPostproxyWebhookService webhooks,
    IOptions<PostproxyOptions> options,
    TimeProvider clock,
    ILogger<PostproxyIntegrationController> logger) : ControllerBase
{
    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    [HttpGet("callback")]
    [ProducesResponseType<ConnectionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? state,
        [FromQuery] string? id,
        [FromQuery] bool failure,
        [FromQuery(Name = "error_code")] string? errorCode,
        CancellationToken cancellationToken)
    {
        var result = await connections.CompleteAsync(state, id, failure, errorCode, cancellationToken);

        var app = options.Value.AppRedirectUrl;
        if (string.IsNullOrWhiteSpace(app))
            return result.Success ? Ok(result) : BadRequest(result);

        var query = result.Success
            ? $"status=connected&platform={result.PlatformCode}&accountId={result.SocialAccountId}"
            : $"status=failed&platform={result.PlatformCode}&error={Uri.EscapeDataString(result.Error ?? "")}";
        return Redirect($"{app}{(app.Contains('?') ? '&' : '?')}{query}");
    }

    [HttpPost("webhooks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers[PostproxyWebhookSignature.HeaderName].ToString();
        if (!PostproxyWebhookSignature.IsValid(signature, body, secret, clock.GetUtcNow(), SignatureTolerance))
        {
            logger.LogWarning("Rejected a Postproxy webhook with an invalid signature.");
            return Unauthorized();
        }

        JsonObject? webhookEvent;
        try
        {
            webhookEvent = JsonNode.Parse(body) as JsonObject;
        }
        catch (JsonException)
        {
            return BadRequest();
        }
        if (webhookEvent is null)
            return BadRequest();

        var outcome = await webhooks.HandleAsync(webhookEvent, cancellationToken);
        logger.LogInformation("Postproxy webhook {EventId} {Type}: {Outcome}", webhookEvent["id"], webhookEvent["type"], outcome);
        return Ok(new { received = true, outcome });
    }
}
