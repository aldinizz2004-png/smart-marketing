namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

public static class PostproxyPlatforms
{
    private static readonly Dictionary<string, string> OursToTheirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["facebook"] = "facebook",
        ["instagram"] = "instagram",
        ["tiktok"] = "tiktok",
        ["linkedin"] = "linkedin",
        ["youtube"] = "youtube",
        ["x"] = "twitter",
        ["whatsapp"] = "whatsapp"
    };

    public static string? ToPostproxy(string platformCode) =>
        OursToTheirs.TryGetValue(platformCode, out var platform) ? platform : null;

    public static string? FromPostproxy(string platform) =>
        OursToTheirs.FirstOrDefault(p => string.Equals(p.Value, platform, StringComparison.OrdinalIgnoreCase)).Key;
}
