namespace SmartMarketingPlatform.Common.Competitors;

public sealed class CompetitorOptions
{
    public const string SectionName = "Competitors";

    public string Provider { get; set; } = "Fake";
    public bool WorkerEnabled { get; set; } = true;
    public int CheckEveryMinutes { get; set; } = 60;
    public int ScanEveryHours { get; set; } = 24;
    public int AnalyzeEveryDays { get; set; } = 7;
    public int MaxPostsPerScan { get; set; } = 25;
    public int MaxSourcesPerRun { get; set; } = 20;
    public int MinPostsForAnalysis { get; set; } = 6;
    public int AnalysisWindowDays { get; set; } = 90;

    // Share of our 3-word phrases that may also appear in one competitor post (0.5 = half).
    public double MaxCopySimilarity { get; set; } = 0.5;

    public InstagramDiscoveryOptions Instagram { get; set; } = new();
}

// Business Discovery reads other business/creator accounts through OUR Instagram professional account.
// BusinessAccountId is that account's IG user id; the token needs instagram_basic + instagram_manage_insights.
public sealed class InstagramDiscoveryOptions
{
    public string GraphBaseUrl { get; set; } = "https://graph.facebook.com/";
    public string ApiVersion { get; set; } = "v23.0";
    public string BusinessAccountId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 20;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BusinessAccountId) && !string.IsNullOrWhiteSpace(AccessToken);
}
