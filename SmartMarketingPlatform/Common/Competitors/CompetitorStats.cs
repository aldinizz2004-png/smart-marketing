using System.Globalization;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Competitors;

public sealed record StatPost(
    Guid Id,
    Guid SourceId,
    string SourceName,
    ContentFormat Format,
    HookType? Hook,
    DateTimeOffset? PostedAt,
    string? Caption,
    decimal? EngagementRate,
    int Interactions);

// Groups the posts by format, hook, time of day and weekday (in the business time zone)
// and compares each group with the overall average.
public static class CompetitorStats
{
    private const int TopPostCount = 5;
    private const int ExcerptLength = 160;

    public static CompetitorFacts Build(IReadOnlyList<StatPost> posts, TimeZoneInfo tz)
    {
        // Rates when every post has one; mixing rates and raw counts would compare apples with oranges.
        var useRate = posts.Count > 0 && posts.All(p => p.EngagementRate is not null);
        decimal Score(StatPost p) => useRate ? p.EngagementRate!.Value : p.Interactions;

        var average = posts.Count == 0 ? 0 : posts.Average(Score);
        var sources = posts.Select(p => p.SourceId).Distinct().Count();

        List<PatternGroup> Group(Func<StatPost, string?> key) => posts
            .Select(p => (Key: key(p), Post: p))
            .Where(x => x.Key is not null)
            .GroupBy(x => x.Key!)
            .Select(g =>
            {
                var avg = g.Average(x => Score(x.Post));
                return new PatternGroup(g.Key, g.Count(), Round(avg, useRate), average == 0 ? 0 : Math.Round(avg / average, 2));
            })
            .OrderByDescending(g => g.AverageScore).ThenByDescending(g => g.Posts)
            .ToList();

        DateTimeOffset? Local(StatPost p) => p.PostedAt is { } t ? TimeZoneInfo.ConvertTime(t, tz) : null;

        var dated = posts.Where(p => p.PostedAt is not null).ToList();
        var perWeek = 0m;
        if (dated.Count > 0 && sources > 0)
        {
            var spanDays = Math.Max(7, (dated.Max(p => p.PostedAt!.Value) - dated.Min(p => p.PostedAt!.Value)).TotalDays + 1);
            perWeek = Math.Round(dated.Count / (decimal)(spanDays / 7) / sources, 1);
        }

        var hashtags = posts
            .SelectMany(p => PostText.Hashtags(p.Caption))
            .GroupBy(h => h).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Take(8).Select(g => g.Key).ToList();

        var top = posts.OrderByDescending(Score).Take(TopPostCount).Select(p => new SamplePost(
            p.Id, p.SourceName, p.Format.ToString(), p.Hook?.ToString(),
            Local(p) is { } t ? t.ToString("ddd HH:mm", CultureInfo.InvariantCulture) : "unknown",
            Round(Score(p), useRate),
            Excerpt(p.Caption))).ToList();

        return new CompetitorFacts(
            sources,
            posts.Count,
            useRate ? "engagement rate (interactions / followers)" : "interactions per post (likes + comments)",
            Round(average, useRate),
            perWeek,
            posts.Count == 0 ? 0 : Math.Round(posts.Count(p => PostText.MentionsOffer(p.Caption)) / (decimal)posts.Count, 2),
            Group(p => p.Format.ToString()),
            Group(p => p.Hook?.ToString()),
            Group(p => Local(p) is { } t ? TimeOfDay(t.Hour) : null),
            Group(p => Local(p)?.DayOfWeek.ToString()),
            hashtags,
            top);
    }

    public static string TimeOfDay(int hour) => hour switch
    {
        >= 6 and < 10 => "06-10",
        >= 10 and < 14 => "10-14",
        >= 14 and < 18 => "14-18",
        >= 18 and < 22 => "18-22",
        _ => "22-06"
    };

    private static decimal Round(decimal value, bool rate) => Math.Round(value, rate ? 4 : 1);

    private static string? Excerpt(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption)) return null;
        var firstLines = string.Join(" ", caption.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(2)).Trim();
        return firstLines.Length <= ExcerptLength ? firstLines : firstLines[..ExcerptLength] + "…";
    }
}
