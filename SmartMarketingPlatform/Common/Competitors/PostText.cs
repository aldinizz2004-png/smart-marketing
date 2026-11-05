using System.Text;
using System.Text.RegularExpressions;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Competitors;

// Plain text rules shared by the scanner and the copy guard. No AI: the same caption always gives
// the same answer, so the numbers the AI explains are computed by code and can be tested.
public static partial class PostText
{
    private static readonly string[] OfferWords = ["%", "خصم", "عرض", "تخفيض", "بلاش", "مجانا", "مجاناً", "discount", "sale", " off", "free", "deal"];
    private static readonly string[] UrgencyWords = ["آخر فرصة", "اخر فرصة", "اليوم بس", "اليوم فقط", "لفترة محدودة", "الكمية محدودة", "last chance", "today only", "limited", "ends tonight"];
    private static readonly string[] TutorialWords = ["كيف", "طريقة", "خطوات", "نصائح", "نصيحة", "how to", "tips", "step", "recipe"];
    private static readonly string[] SocialProofWords = ["زبائن", "زباين", "زبوناتنا", "تقييم", "آراء", "customers", "reviews", "rated", "⭐"];
    private static readonly string[] StoryWords = ["قصة", "حكاية", "كواليس", "بدأنا", "story", "behind the scenes", "journey"];
    private static readonly string[] HumorWords = ["😂", "🤣", "ههه", "lol", "haha"];

    public static HookType? DetectHook(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption)) return null;
        var text = caption.Trim();
        var firstLine = text.Split('\n', 2)[0];

        bool Has(string[] words) => words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));

        if (Has(UrgencyWords)) return HookType.Urgency;
        if (Has(OfferWords)) return HookType.Offer;
        if (firstLine.Contains('?') || firstLine.Contains('؟')) return HookType.Question;
        if (Has(TutorialWords)) return HookType.Tutorial;
        if (Has(SocialProofWords)) return HookType.SocialProof;
        if (Has(StoryWords)) return HookType.Story;
        if (Has(HumorWords)) return HookType.Humor;
        return HookType.Informative;
    }

    public static bool MentionsOffer(string? caption) =>
        caption is not null && OfferWords.Any(w => caption.Contains(w, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> Hashtags(string? caption) =>
        caption is null
            ? []
            : HashtagPattern().Matches(caption).Select(m => m.Value.ToLowerInvariant()).Distinct().Take(30).ToList();

    // How much of OUR text also appears in THEIR text: shared 3-word phrases / our 3-word phrases.
    // 1.0 = every phrase of ours is in their post; a few common words in a row are not enough.
    public static double Containment(string ours, string theirs)
    {
        var mine = Shingles(ours);
        if (mine.Count < MinShingles) return 0;
        var other = Shingles(theirs);
        if (other.Count == 0) return 0;
        return (double)mine.Count(other.Contains) / mine.Count;
    }

    private const int MinShingles = 3;

    private static HashSet<string> Shingles(string text)
    {
        var words = Words(text);
        var set = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i + 2 < words.Count; i++)
            set.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        return set;
    }

    // Lower case, no hashtags/mentions/links, Arabic letter variants and diacritics unified.
    internal static List<string> Words(string text)
    {
        var cleaned = NoisePattern().Replace(text, " ");
        var sb = new StringBuilder(cleaned.Length);
        foreach (var ch in cleaned.ToLowerInvariant())
        {
            var c = ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',   // أ إ آ ٱ -> ا
                'ة' => 'ه',                                       // ة -> ه
                'ى' or 'ئ' => 'ي',                           // ى ئ -> ي
                'ؤ' => 'و',                                       // ؤ -> و
                _ => ch
            };
            if (c == 'ـ' || (c >= 'ً' && c <= 'ٟ') || c == 'ٰ') continue;   // tatweel, harakat
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    [GeneratedRegex(@"#[\p{L}\p{N}_]+")]
    private static partial Regex HashtagPattern();

    [GeneratedRegex(@"https?://\S+|[#@][\p{L}\p{N}_.]+")]
    private static partial Regex NoisePattern();
}
