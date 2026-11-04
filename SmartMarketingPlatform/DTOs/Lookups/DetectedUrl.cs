namespace SmartMarketingPlatform.DTOs.Lookups;

// Result of detecting the platform of a pasted URL (used later by competitor tracking).
public sealed record DetectedUrl(string Url, string NormalizedUrl, string Host, string PlatformCode, string? Handle);
