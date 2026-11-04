using System.Security.Cryptography;
using System.Text;

namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

// Header "X-Postproxy-Signature: t=<unix seconds>,v1=<hex HMAC-SHA256 of "{t}.{raw body}">".
public static class PostproxyWebhookSignature
{
    public const string HeaderName = "X-Postproxy-Signature";

    public static bool IsValid(string? header, string body, string secret, DateTimeOffset now, TimeSpan tolerance)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrEmpty(secret))
            return false;

        string? timestamp = null;
        var signatures = new List<string>();
        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2) continue;
            if (pair[0] == "t") timestamp = pair[1];
            else if (pair[0] == "v1") signatures.Add(pair[1]);
        }

        if (timestamp is null || signatures.Count == 0 || !long.TryParse(timestamp, out var seconds))
            return false;
        if ((now - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > tolerance)
            return false;

        var expected = Compute(secret, timestamp, body);
        return signatures.Any(s => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(s.ToLowerInvariant()), Encoding.ASCII.GetBytes(expected)));
    }

    public static string Compute(string secret, string timestamp, string body)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        return Convert.ToHexStringLower(hash);
    }
}
