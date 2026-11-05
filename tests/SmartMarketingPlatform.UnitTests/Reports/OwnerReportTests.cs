using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Common.Reports;
using SmartMarketingPlatform.UnitTests.Ai;

namespace SmartMarketingPlatform.UnitTests.Reports;

public class OwnerReportTests
{
    private static MarketingContext Context(string dialect = "Palestinian Arabic", params string[] prohibited) => new(
        Guid.NewGuid(), "Qahwet Al-Hara", "Cafe", "Nablus", "Asia/Hebron", "ILS", dialect, "friendly", 1,
        [], prohibited, [], [], [], ["instagram"], 2, null, null, null);

    private static OwnerReportFacts Facts(int posts = 4, int openComplaints = 1, int awaitingReview = 2) => new(
        "2026-10-25", "2026-11-01", posts, 2, 1800m, 1200m, 0.062m, 0.05m,
        new ReportGroup("instagram", 3, 0.07m), new ReportGroup("Reel", 2, 0.09m), new ReportGroup("Question", 2, 0.08m),
        new ReportTopPost("instagram", "Reel", "Question", 950m, 0.09m, "https://instagram.com/p/x", "<b>New</b> pistachio latte"),
        5, new Dictionary<string, int> { ["Praise"] = 3, ["Complaint"] = 1, ["Question"] = 1 },
        openComplaints, 1, awaitingReview, 0, 1, ["20% off knafeh"]);

    [Fact]
    public async Task Fake_report_turns_facts_into_wins_concerns_and_actions()
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));

        var draft = (await ai.WriteOwnerReportAsync(Context(dialect: "English"), Facts(), CancellationToken.None)).Output;

        Assert.Contains("+50%", draft.Headline);
        Assert.Contains(draft.Wins, w => w.Contains("Reel"));
        Assert.Contains(draft.Concerns, c => c.Contains("1 complaint"));
        Assert.Contains(draft.NextActions, a => a.Contains("2 post(s) waiting"));
        Assert.Contains(draft.NextActions, a => a.Contains("20% off knafeh"));
    }

    [Fact]
    public async Task Fake_report_warns_when_nothing_was_published()
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));

        var draft = (await ai.WriteOwnerReportAsync(Context(dialect: "English"), Facts(posts: 0, openComplaints: 0, awaitingReview: 0), CancellationToken.None)).Output;

        Assert.Contains(draft.Concerns, c => c.Contains("Nothing was published"));
        Assert.NotEmpty(draft.NextActions);
    }

    [Fact]
    public async Task Llm_report_without_next_actions_goes_to_the_next_model()
    {
        var empty = """{"headline":"Good week","summary":"Reach went up.","wins":["Reels"],"concerns":[],"nextActions":[]}""";
        var good = """{"headline":"Good week","summary":"Reach went up by half.","wins":["Reels worked"],"concerns":["1 complaint open"],"nextActions":["Answer the complaint","Post two reels"]}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Returns(empty)),
            new StubChatModel("Groq", "b", StubChatModel.Returns(good))));

        var result = await ai.WriteOwnerReportAsync(Context(), Facts(), CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Equal(["Answer the complaint", "Post two reels"], result.Output.NextActions);
    }

    [Fact]
    public async Task Llm_report_keeps_at_most_five_actions_and_drops_empty_items()
    {
        var answer = """{"headline":"Week","summary":"Ok.","wins":["", "Reels"],"concerns":[],"nextActions":["a","b","c","d","e","f","g"]}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var draft = (await ai.WriteOwnerReportAsync(Context(), Facts(), CancellationToken.None)).Output;

        Assert.Equal(5, draft.NextActions.Count);
        Assert.Equal(["Reels"], draft.Wins);
    }

    [Fact]
    public async Task The_prompt_carries_the_real_numbers()
    {
        var model = new StubChatModel("Gemini", "a", StubChatModel.Returns("""{"headline":"h","summary":"s","nextActions":["a"]}"""));
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(model));

        await ai.WriteOwnerReportAsync(Context(), Facts(), CancellationToken.None);

        Assert.Contains("\"reach\":1800", model.LastRequest!.User);
        Assert.Contains("\"unansweredComplaints\":1", model.LastRequest.User);
    }

    [Fact]
    public void Email_is_right_to_left_in_arabic_and_escapes_html()
    {
        var draft = new OwnerReportDraft("أسبوع منيح", "<script>x</script>", ["ريلز"], [], ["رد على الشكوى"]);

        var html = ReportEmailRenderer.Render("قهوة الحارة", draft, Facts(), arabic: true);

        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("6.2%", html);
        Assert.Contains("+50%", html);
    }

    [Theory]
    [InlineData(150, 100, "+50%")]
    [InlineData(80, 100, "-20%")]
    [InlineData(100, 100, "0%")]
    [InlineData(10, 0, "new")]
    [InlineData(0, 0, "-")]
    public void Change_is_shown_against_the_previous_period(int now, int before, string expected)
    {
        Assert.Equal(expected, ReportEmailRenderer.Change(now, before));
    }
}
