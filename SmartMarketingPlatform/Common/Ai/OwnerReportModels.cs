namespace SmartMarketingPlatform.Common.Ai;

// Every number in a report comes from here (the database); the AI only reads and explains them.
public sealed record OwnerReportFacts(
    string PeriodStartLocal,
    string PeriodEndLocal,
    int Posts,
    int PreviousPosts,
    decimal Reach,
    decimal PreviousReach,
    decimal EngagementRate,
    decimal PreviousEngagementRate,
    ReportGroup? BestPlatform,
    ReportGroup? BestFormat,
    ReportGroup? BestHook,
    ReportTopPost? TopPost,
    int NewComments,
    IReadOnlyDictionary<string, int> CommentsByCategory,
    int UnansweredComplaints,
    int UnansweredQuestions,
    int AwaitingReview,
    int ApprovedNotScheduled,
    int ScheduledNext7Days,
    IReadOnlyList<string> OffersEndingSoon);

public sealed record ReportGroup(string Key, int Posts, decimal EngagementRate);

public sealed record ReportTopPost(string Platform, string Format, string? Hook, decimal Reach, decimal EngagementRate, string? Url, string? Excerpt);

public sealed record OwnerReportDraft(
    string Headline,
    string Summary,
    IReadOnlyList<string> Wins,
    IReadOnlyList<string> Concerns,
    IReadOnlyList<string> NextActions);
