using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Integrations.Instagram;

// Instagram Graph API "Business Discovery": public profile and recent media of another business or
// creator account, read through our own professional account. Personal accounts are not visible.
public sealed partial class InstagramDiscoverySource(
    HttpClient http,
    IOptions<CompetitorOptions> options,
    ILogger<InstagramDiscoverySource> logger) : ICompetitorSource
{
    private const string MediaFields = "id,caption,media_type,media_product_type,permalink,timestamp,like_count,comments_count";

    public bool Supports(string platformCode) => platformCode == "instagram";

    public async Task<CompetitorSnapshot> FetchAsync(CompetitorTarget target, int maxPosts, CancellationToken cancellationToken)
    {
        var settings = options.Value.Instagram;
        if (!settings.IsConfigured)
            throw new CompetitorSourceException("not_configured", "Instagram Business Discovery is not configured (Competitors:Instagram).");
        if (!HandlePattern().IsMatch(target.Handle))
            throw new CompetitorSourceException("invalid_handle", $"'{target.Handle}' is not a valid Instagram username.", sourceProblem: true);

        var limit = Math.Clamp(maxPosts, 1, 50);
        var fields = $"business_discovery.username({target.Handle}){{username,name,followers_count,media_count,media.limit({limit}){{{MediaFields}}}}}";
        var url = $"{settings.ApiVersion}/{Uri.EscapeDataString(settings.BusinessAccountId)}?fields={Uri.EscapeDataString(fields)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", settings.AccessToken);   // header, so the token never appears in logged URLs

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new CompetitorSourceException("unavailable", $"Instagram did not answer: {ex.Message}");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = ToError(response.StatusCode, body);
                logger.LogWarning("Business Discovery failed for {Handle}: {Code} {Message}", target.Handle, error.Code, error.Message);
                throw error;
            }

            var root = JsonNode.Parse(body) as JsonObject
                       ?? throw new CompetitorSourceException("invalid_response", "Instagram returned an unexpected answer.");
            return Map(root);
        }
    }

    internal static CompetitorSnapshot Map(JsonObject root)
    {
        var account = root["business_discovery"] as JsonObject
                      ?? throw new CompetitorSourceException("not_found", "Instagram returned no account.", sourceProblem: true);

        var posts = new List<CompetitorPost>();
        foreach (var media in (account["media"]?["data"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
        {
            var id = media["id"]?.ToString();
            if (string.IsNullOrEmpty(id)) continue;
            posts.Add(new CompetitorPost(
                id,
                media["permalink"]?.ToString(),
                ParseTime(media["timestamp"]?.ToString()),
                ToFormat(media["media_type"]?.ToString(), media["media_product_type"]?.ToString()),
                media["caption"]?.ToString(),
                Int(media["like_count"]),
                Int(media["comments_count"]),
                null));
        }

        var name = account["name"]?.ToString();
        return new CompetitorSnapshot(
            string.IsNullOrWhiteSpace(name) ? account["username"]?.ToString() : name,
            account["followers_count"] is JsonValue f && f.TryGetValue<long>(out var followers) ? followers : null,
            posts);
    }

    internal static ContentFormat ToFormat(string? mediaType, string? productType) => (mediaType?.ToUpperInvariant(), productType?.ToUpperInvariant()) switch
    {
        (_, "REELS") => ContentFormat.Reel,
        (_, "STORY") => ContentFormat.Story,
        ("CAROUSEL_ALBUM", _) => ContentFormat.Carousel,
        ("VIDEO", _) => ContentFormat.Video,
        _ => ContentFormat.SingleImage
    };

    // Graph errors: 190 = bad token, 4/17/32/613 = rate limits, 100/110 = the username does not exist
    // or is not a business/creator account.
    internal static CompetitorSourceException ToError(HttpStatusCode status, string body)
    {
        var error = (JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body) as JsonObject)?["error"] as JsonObject;
        var code = error?["code"] is JsonValue c && c.TryGetValue<int>(out var n) ? n : 0;
        var message = error?["message"]?.ToString() ?? $"HTTP {(int)status}";

        if (code == 190 || status == HttpStatusCode.Unauthorized)
            return new CompetitorSourceException("auth_failed", "The Instagram access token is invalid or expired.");
        if (code is 4 or 17 or 32 or 613 || status == HttpStatusCode.TooManyRequests)
            return new CompetitorSourceException("rate_limited", "Instagram rate limit reached; try again later.");
        if (code is 100 or 110)
            return new CompetitorSourceException("not_found", "Instagram cannot show this account. It must be a public business or creator account.", sourceProblem: true);
        if ((int)status >= 500)
            return new CompetitorSourceException("unavailable", $"Instagram is unavailable: {message}");
        return new CompetitorSourceException("bad_request", message);
    }

    // Graph timestamps look like "2026-10-01T18:20:00+0000".
    internal static DateTimeOffset? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var fixedOffset = OffsetPattern().Replace(value.Trim(), "$1:$2");
        return DateTimeOffset.TryParse(fixedOffset, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var t) ? t.ToUniversalTime() : null;
    }

    private static int? Int(JsonNode? node) => node is JsonValue v && v.TryGetValue<int>(out var n) ? n : null;

    [GeneratedRegex(@"^[A-Za-z0-9._]{1,30}$")]
    private static partial Regex HandlePattern();

    [GeneratedRegex(@"([+-]\d{2})(\d{2})$")]
    private static partial Regex OffsetPattern();
}
