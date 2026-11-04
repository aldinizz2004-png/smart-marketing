using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Lookups;

namespace SmartMarketingPlatform.Services;

// Turns any URL the owner pastes (competitor, professional account ...) into
// a normalized URL, its host and its platform. The host list lives in the
// PlatformDomains table, so supporting a new network is a data change only.
public class PlatformDetector
{
    public const string FallbackPlatform = "website";

    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
        "fbclid", "gclid", "igshid", "si"
    };

    private readonly MarketingDbContext _db;

    public PlatformDetector(MarketingDbContext db) => _db = db;

    public async Task<DetectedUrl> DetectAsync(string rawUrl, CancellationToken ct = default)
    {
        var withScheme = rawUrl.Contains("://", StringComparison.Ordinal) ? rawUrl.Trim() : "https://" + rawUrl.Trim();

        if (!Uri.TryCreate(withScheme, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !uri.Host.Contains('.'))
        {
            throw new BadRequestException("Not a valid http(s) URL.");
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
        if (host.StartsWith("m.", StringComparison.Ordinal)) host = host[2..];

        // Match the host or a parent domain (business.facebook.com -> facebook.com).
        var parts = host.Split('.');
        var candidates = Enumerable.Range(0, parts.Length - 1).Select(i => string.Join('.', parts[i..])).ToList();

        var platformCode = await _db.PlatformDomains
            .Where(d => candidates.Contains(d.Domain) && d.Platform.IsActive)
            .OrderByDescending(d => d.Domain.Length)
            .Select(d => d.PlatformCode)
            .FirstOrDefaultAsync(ct) ?? FallbackPlatform;

        // Drop tracking parameters so the same page is stored once.
        var query = string.Join('&', uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !TrackingParameters.Contains(p.Split('=')[0])));

        var path = uri.AbsolutePath.TrimEnd('/');   // path kept as-is: some ids are case-sensitive
        var normalized = $"https://{host}{path}{(query.Length > 0 ? "?" + query : string.Empty)}";

        // On social platforms the first path segment is the account handle (instagram.com/{handle}).
        var handle = platformCode != FallbackPlatform && uri.Segments.Length > 1
            ? uri.Segments[1].Trim('/').TrimStart('@')
            : null;

        return new DetectedUrl(rawUrl, normalized, host, platformCode, string.IsNullOrEmpty(handle) ? null : handle);
    }
}
