using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Integrations.WhatsApp;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Called by Meta, never with our JWT. GET verifies the webhook once; POST brings messages and delivery statuses.
[ApiController]
[Route("api/integrations/whatsapp/webhook")]
[AllowAnonymous]
[DisableRateLimiting]
public class WhatsAppIntegrationController(
    IWhatsAppInboxService inbox,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppIntegrationController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = options.Value.VerifyToken;
        var valid = mode == "subscribe" && !string.IsNullOrEmpty(expected) && token is not null
                    && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(expected));
        return valid ? Content(challenge ?? string.Empty, "text/plain") : StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = options.Value.AppSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return NotFound();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (!WhatsAppWebhook.IsValidSignature(Request.Headers[WhatsAppWebhook.SignatureHeader].ToString(), body, secret))
        {
            logger.LogWarning("Rejected a WhatsApp webhook with an invalid signature.");
            return Unauthorized();
        }

        JsonObject? root;
        try { root = JsonNode.Parse(body) as JsonObject; }
        catch (JsonException) { return BadRequest(); }
        if (root is null) return BadRequest();

        var (messages, statuses) = await inbox.HandleWebhookAsync(WhatsAppWebhook.Parse(root), cancellationToken);
        return Ok(new { received = true, messages, statuses });
    }
}
