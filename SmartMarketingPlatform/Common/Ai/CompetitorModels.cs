using System.Text.Json;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// Numbers computed by code from the collected posts. The AI only explains them and turns them into
// advice for OUR business; it never sees a whole competitor post, only a short excerpt of the best ones.
// Score = engagement rate (interactions / followers) when followers are known, else interactions per post.
public sealed record CompetitorFacts(
    int Sources,
    int PostsAnalyzed,
    string ScoreMeaning,
    decimal AverageScore,
    decimal PostsPerWeekPerSource,
    decimal OfferShare,
    IReadOnlyList<PatternGroup> ByFormat,
    IReadOnlyList<PatternGroup> ByHook,
    IReadOnlyList<PatternGroup> ByTimeOfDay,
    IReadOnlyList<PatternGroup> ByWeekday,
    IReadOnlyList<string> TopHashtags,
    IReadOnlyList<SamplePost> TopPosts);

// Lift = group average / overall average (2.0 = twice the usual engagement).
public sealed record PatternGroup(string Key, int Posts, decimal AverageScore, decimal Lift);

public sealed record SamplePost(Guid Id, string Source, string Format, string? Hook, string LocalTime, decimal Score, string? Excerpt);

public sealed record CompetitorInsightDraft(
    InsightType Type,
    string Summary,
    string ApplyToUs,
    decimal Confidence,
    IReadOnlyList<Guid> EvidencePostIds);

// A saved insight as the AI sees it later, when it plans or recommends.
public sealed record InsightInfo(Guid Id, InsightType Type, string Summary, string? ApplyToUs);

// What is kept in CompetitorInsight.DetailsJson besides the summary.
public sealed record CompetitorInsightDetails(string ApplyToUs, IReadOnlyList<Guid> EvidencePostIds, Guid ExecutionId)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static CompetitorInsightDetails? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<CompetitorInsightDetails>(json, Json); }
        catch (JsonException) { return null; }
    }

    public static string? ReadApplyToUs(string? json) => Parse(json)?.ApplyToUs;
}
