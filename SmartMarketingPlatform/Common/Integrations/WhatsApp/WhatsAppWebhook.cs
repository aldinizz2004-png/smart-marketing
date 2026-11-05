using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Common.Integrations.WhatsApp;

public sealed record InboundWhatsAppMessage(
    string PhoneNumberId, string From, string? ProfileName, string MessageId, DateTimeOffset SentAt, string Type, string Text);

public sealed record WhatsAppStatusUpdate(string PhoneNumberId, string MessageId, string Status, DateTimeOffset At, string? Error);

public sealed record WhatsAppWebhookEvent(IReadOnlyList<InboundWhatsAppMessage> Messages, IReadOnlyList<WhatsAppStatusUpdate> Statuses);

// Meta sends: entry[].changes[].value { metadata.phone_number_id, contacts[], messages[], statuses[] }.
public static class WhatsAppWebhook
{
    public const string SignatureHeader = "X-Hub-Signature-256";

    // "sha256=" + hex HMAC-SHA256 of the raw body with the app secret.
    public static bool IsValidSignature(string? header, string body, string appSecret)
    {
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) return false;
        byte[] given;
        try { given = Convert.FromHexString(header[7..]); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(given, Sign(body, appSecret));
    }

    public static string ComputeSignature(string body, string appSecret) => "sha256=" + Convert.ToHexStringLower(Sign(body, appSecret));

    private static byte[] Sign(string body, string appSecret) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(body));

    public static WhatsAppWebhookEvent Parse(JsonObject root)
    {
        var messages = new List<InboundWhatsAppMessage>();
        var statuses = new List<WhatsAppStatusUpdate>();

        foreach (var change in (root["entry"] as JsonArray ?? new JsonArray())
                     .SelectMany(e => e?["changes"] as JsonArray ?? new JsonArray()))
        {
            if (change?["value"] is not JsonObject value) continue;
            var phoneNumberId = value["metadata"]?["phone_number_id"]?.ToString();
            if (string.IsNullOrEmpty(phoneNumberId)) continue;

            var names = (value["contacts"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Where(c => c["wa_id"] is not null)
                .GroupBy(c => c["wa_id"]!.ToString())
                .ToDictionary(g => g.Key, g => g.First()["profile"]?["name"]?.ToString());

            foreach (var m in (value["messages"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                var from = m["from"]?.ToString();
                var id = m["id"]?.ToString();
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(id)) continue;
                var type = m["type"]?.ToString() ?? "unknown";
                messages.Add(new InboundWhatsAppMessage(
                    phoneNumberId, from, names.GetValueOrDefault(from), id, UnixTime(m["timestamp"]), type, TextOf(m, type)));
            }

            foreach (var s in (value["statuses"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                var id = s["id"]?.ToString();
                var status = s["status"]?.ToString();
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status)) continue;
                var error = (s["errors"] as JsonArray)?.FirstOrDefault() as JsonObject;
                statuses.Add(new WhatsAppStatusUpdate(phoneNumberId, id, status, UnixTime(s["timestamp"]),
                    error is null ? null : $"{error["code"]}: {error["title"] ?? error["message"]}"));
            }
        }

        return new WhatsAppWebhookEvent(messages, statuses);
    }

    // What a person would read. Media is shown as a label (with its caption), so the AI and the owner know it arrived.
    private static string TextOf(JsonObject m, string type) => type switch
    {
        "text" => m["text"]?["body"]?.ToString() ?? string.Empty,
        "button" => m["button"]?["text"]?.ToString() ?? string.Empty,
        "interactive" => m["interactive"]?["button_reply"]?["title"]?.ToString()
                         ?? m["interactive"]?["list_reply"]?["title"]?.ToString() ?? string.Empty,
        "location" => $"[location] {m["location"]?["name"] ?? m["location"]?["address"] ?? ""}".Trim(),
        "image" or "video" or "document" or "audio" or "sticker" => $"[{type}] {m[type]?["caption"]}".Trim(),
        _ => $"[{type}]"
    };

    private static DateTimeOffset UnixTime(JsonNode? node) =>
        long.TryParse(node?.ToString(), out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : DateTimeOffset.UtcNow;
}
