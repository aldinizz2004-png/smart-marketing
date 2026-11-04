namespace SmartMarketingPlatform.DTOs.Lookups;

public sealed record PlatformResponse(
    string Code,
    string DisplayNameKey,
    string DisplayName,
    bool SupportsPublishing,
    bool SupportsMessaging,
    bool SupportsCompetitorTracking,
    IReadOnlyList<string> Domains);
