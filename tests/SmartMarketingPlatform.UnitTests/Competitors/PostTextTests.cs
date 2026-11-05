using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Competitors;

public class PostTextTests
{
    [Theory]
    [InlineData("شو أطيب إشي بتطلبوه الصبح؟\nتعالوا جربوا", HookType.Question)]
    [InlineData("خصم ٢٠٪ على كل المشروبات", HookType.Offer)]
    [InlineData("آخر فرصة! العرض بخلص الليلة", HookType.Urgency)]
    [InlineData("٥ نصائح لتختار الهدية المناسبة", HookType.Tutorial)]
    [InlineData("آراء زبائننا بعد أول أسبوع", HookType.SocialProof)]
    [InlineData("كواليس المطبخ من الصبح", HookType.Story)]
    [InlineData("وصلت التشكيلة الجديدة", HookType.Informative)]
    public void Detects_the_hook_of_a_caption(string caption, HookType expected)
    {
        Assert.Equal(expected, PostText.DetectHook(caption));
    }

    [Fact]
    public void An_empty_caption_has_no_hook()
    {
        Assert.Null(PostText.DetectHook("  "));
    }

    [Fact]
    public void Extracts_hashtags_once_in_lower_case()
    {
        var tags = PostText.Hashtags("قهوة الصبح #Coffee #قهوة #coffee");

        Assert.Equal(new[] { "#coffee", "#قهوة" }, tags);
    }

    [Fact]
    public void The_same_caption_is_a_full_copy()
    {
        const string caption = "جربتوا الكنافة الجديدة عنا؟ طازة كل يوم من الصبح";

        Assert.Equal(1.0, PostText.Containment(caption, caption));
    }

    [Fact]
    public void Arabic_spelling_variants_and_hashtags_do_not_hide_a_copy()
    {
        var theirs = "أحلى كنافة بالمدينة طازة كل يوم من الصبح #كنافة";
        var ours = "احلى كنافه بالمدينه طازه كل يوم من الصبح";

        Assert.True(PostText.Containment(ours, theirs) >= 0.99);
    }

    [Fact]
    public void Sharing_a_few_common_words_is_not_copying()
    {
        var theirs = "كل يوم من الصبح عنا قهوة طازة وكيك بالشوكولاتة";
        var ours = "لاتيه الفستق الجديد بـ ١٤ شيكل، اطلبه هلأ وخليك معنا";

        Assert.True(PostText.Containment(ours, theirs) < 0.2);
    }

    [Fact]
    public void A_very_short_text_is_never_counted_as_a_copy()
    {
        Assert.Equal(0.0, PostText.Containment("اطلب هلأ", "اطلب هلأ"));
    }
}
