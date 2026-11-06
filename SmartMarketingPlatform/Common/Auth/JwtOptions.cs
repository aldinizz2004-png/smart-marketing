namespace SmartMarketingPlatform.Common.Auth;

// Bound from the "Jwt" section. The Key is a secret: in Development it lives in
// appsettings.Development.json; in production it comes from the Jwt__Key environment variable.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;   // HMAC-SHA256 needs at least 256 bits

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 15;               // access token: short, so a stolen one dies fast
    public int RefreshTokenDays { get; init; } = 30;            // refresh token: renewed on every use

    // Two requests that refresh with the same token at once (two browser tabs) are not an attack.
    public int RefreshReuseGraceSeconds { get; init; } = 30;
}
