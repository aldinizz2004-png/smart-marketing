namespace SmartMarketingPlatform.Common.Options;

// "Comments" section: the background analysis of new comments.
public sealed class CommentOptions
{
    public const string SectionName = "Comments";

    public bool WorkerEnabled { get; init; } = true;
    public int CheckEverySeconds { get; init; } = 300;
    public int MaxBusinessesPerRun { get; init; } = 20;

    // Email the owners and admins when the analysis finds complaints.
    public bool NotifyComplaints { get; init; } = true;
}
