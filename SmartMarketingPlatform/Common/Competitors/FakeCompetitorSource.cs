using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Competitors;

// Demo data with a clear pattern to learn from: evening reels with a question get the most engagement,
// offers go out on Thursdays. The same handle always gives the same posts.
// A handle containing "notfound" behaves like an account that cannot be read.
public sealed class FakeCompetitorSource(TimeProvider clock) : ICompetitorSource
{
    public const string NotFoundMarker = "notfound";

    private static readonly (ContentFormat Format, string Caption, int Weight)[] Templates =
    [
        (ContentFormat.Reel, "شو أطيب إشي بتطلبوه الصبح؟ ☕\nشوفوا كيف بنحضّر قهوتنا\n#قهوة #صباح", 3),
        (ContentFormat.SingleImage, "وصلت التشكيلة الجديدة 🍰\nزورونا اليوم\n#حلويات", 1),
        (ContentFormat.Carousel, "٥ نصائح لتختار الهدية المناسبة 🎁\nاسحب لتشوف\n#هدايا #نصائح", 2),
        (ContentFormat.Reel, "مين جرب الطعم الجديد؟ 😍\nاحكولنا بالتعليقات\n#جديد", 3),
        (ContentFormat.SingleImage, "خصم ٢٠٪ على كل المشروبات الخميس بس! 🎉\n#عرض #خميس", 2),
        (ContentFormat.Reel, "كواليس المطبخ من الساعة ٦ الصبح 👨‍🍳\n#كواليس", 2)
    ];

    public bool Supports(string platformCode) => platformCode != "website";

    public Task<CompetitorSnapshot> FetchAsync(CompetitorTarget target, int maxPosts, CancellationToken cancellationToken)
    {
        if (target.Handle.Contains(NotFoundMarker, StringComparison.OrdinalIgnoreCase))
            throw new CompetitorSourceException("not_found", $"No public business account named '{target.Handle}'.", sourceProblem: true);

        var seed = StableHash(target.Handle);
        var followers = 4_000 + seed % 6_000;
        var today = clock.GetUtcNow().UtcDateTime.Date;
        var posts = new List<CompetitorPost>();

        for (var i = 0; i < Math.Min(maxPosts, 12); i++)
        {
            var template = Templates[i % Templates.Length];
            var isOffer = template.Caption.Contains("خصم");
            var day = isOffer ? LastThursdayBefore(today.AddDays(-7 * (i / Templates.Length))) : today.AddDays(-(2 * i + 1));
            var utcHour = template.Format == ContentFormat.Reel ? 17 : 9;   // ~20:00 and ~12:00 in Palestine
            var likes = (int)(followers * 0.012 * template.Weight) + (seed + i * 37) % 40;
            var comments = template.Caption.Contains('؟') ? likes / 6 : likes / 15;

            posts.Add(new CompetitorPost(
                $"fake_{target.Handle}_{i}",
                $"{target.NormalizedUrl}/p/fake{i}",
                new DateTimeOffset(day.AddHours(utcHour), TimeSpan.Zero),
                template.Format,
                template.Caption,
                likes,
                comments,
                template.Format == ContentFormat.Reel ? likes * 12L : null));
        }

        return Task.FromResult(new CompetitorSnapshot(target.Handle, followers, posts));
    }

    private static DateTime LastThursdayBefore(DateTime day)
    {
        var back = ((int)day.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return day.AddDays(-(back == 0 ? 7 : back));
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var ch in text.ToLowerInvariant()) hash = (hash ^ ch) * 16777619u;
            return (int)(hash % 100_000);
        }
    }
}
