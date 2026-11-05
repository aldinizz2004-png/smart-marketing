using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Messaging;

namespace SmartMarketingPlatform.Common.Integrations.WhatsApp;

// WhatsApp Cloud API: POST /{version}/{phone-number-id}/messages with the business's own token.
public sealed class MetaWhatsAppGateway(HttpClient http, IOptions<WhatsAppOptions> options) : IWhatsAppGateway
{
    public Task<string> SendTextAsync(WhatsAppSender from, string to, string text, CancellationToken cancellationToken) =>
        SendAsync(from, new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = to,
            ["type"] = "text",
            ["text"] = new JsonObject { ["body"] = text, ["preview_url"] = false }
        }, cancellationToken);

    public Task<string> SendTemplateAsync(WhatsAppSender from, string to, string templateName, string languageCode, CancellationToken cancellationToken) =>
        SendAsync(from, new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = to,
            ["type"] = "template",
            ["template"] = new JsonObject { ["name"] = templateName, ["language"] = new JsonObject { ["code"] = languageCode } }
        }, cancellationToken);

    private async Task<string> SendAsync(WhatsAppSender from, JsonObject payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.Value.ApiVersion}/{Uri.EscapeDataString(from.PhoneNumberId)}/messages")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new("Bearer", from.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new WhatsAppGatewayException("unavailable", $"WhatsApp did not answer: {ex.Message}");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ToError(response.StatusCode, body);

            var id = (JsonNode.Parse(body)?["messages"] as JsonArray)?.FirstOrDefault()?["id"]?.ToString();
            return string.IsNullOrEmpty(id)
                ? throw new WhatsAppGatewayException("invalid_response", "WhatsApp accepted the message but returned no id.")
                : id;
        }
    }

    // 131047 = more than 24 hours since the customer's last message; 190 = bad token;
    // 130429 / 131056 = rate limits; 131026 = the number cannot receive WhatsApp messages.
    internal static WhatsAppGatewayException ToError(HttpStatusCode status, string body)
    {
        JsonObject? error = null;
        try { error = (JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body) as JsonObject)?["error"] as JsonObject; }
        catch (System.Text.Json.JsonException) { }

        var code = error?["code"] is JsonValue c && c.TryGetValue<int>(out var n) ? n : 0;
        var message = error?["message"]?.ToString() ?? $"HTTP {(int)status}";

        return code switch
        {
            131047 => new WhatsAppGatewayException("outside_window", "More than 24 hours passed since the customer's last message; send a template.", outsideWindow: true),
            190 => new WhatsAppGatewayException("auth_failed", "The WhatsApp access token is invalid or expired."),
            130429 or 131056 => new WhatsAppGatewayException("rate_limited", "WhatsApp rate limit reached; try again later."),
            131026 => new WhatsAppGatewayException("undeliverable", "This number cannot receive WhatsApp messages."),
            _ when status == HttpStatusCode.Unauthorized => new WhatsAppGatewayException("auth_failed", "The WhatsApp access token is invalid or expired."),
            _ when (int)status >= 500 => new WhatsAppGatewayException("unavailable", $"WhatsApp is unavailable: {message}"),
            _ => new WhatsAppGatewayException("bad_request", message)
        };
    }
}
