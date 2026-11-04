namespace SmartMarketingPlatform.Common.Options;

// "Publishing" section: how the background worker runs. Development uses short intervals.
public sealed class PublishingOptions
{
    public const string SectionName = "Publishing";

    public bool WorkerEnabled { get; init; } = true;
    public int PollSeconds { get; init; } = 15;             // how often due jobs are looked for
    public int BatchSize { get; init; } = 10;
    public int MaxAttempts { get; init; } = 3;
    public int RetryBaseSeconds { get; init; } = 60;        // 1x, 2x, 4x ... (exponential backoff)
    public int StuckAfterMinutes { get; init; } = 10;       // a Running job older than this is retried
    public int MetricsEverySeconds { get; init; } = 900;    // one metrics snapshot per publication per interval
    public int MetricsWindowDays { get; init; } = 7;        // metrics are collected for this long after publishing
}
