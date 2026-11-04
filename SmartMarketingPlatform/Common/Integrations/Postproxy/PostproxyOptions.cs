namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

public sealed class PostproxyOptions
{
    public const string SectionName = "Postproxy";

    public string BaseUrl { get; init; } = "https://api.postproxy.dev/";
    public string? ApiKey { get; init; }
    public string? WebhookSecret { get; init; }

    // Public address of this API; Postproxy sends the owner back here after the platform login.
    public string? PublicBaseUrl { get; init; }

    // Front-end page that shows the result of a connection. Without it the callback answers with JSON.
    public string? AppRedirectUrl { get; init; }

    public int PublishWaitSeconds { get; init; } = 20;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxMediaBytes { get; init; } = 20 * 1024 * 1024;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
