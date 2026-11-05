using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class CompetitorAnalysisTests
{
    private static readonly Guid ReelPost = Guid.NewGuid();
    private const string TheirCaption = "شو أطيب إشي بتطلبوه الصبح؟ شوفوا كيف بنحضّر قهوتنا كل يوم";

    private static MarketingContext Context(params string[] prohibited) => new(
        Guid.NewGuid(), "Qahwet Al-Hara", "Cafe", "Nablus", "Asia/Hebron", "ILS", "Palestinian Arabic", "friendly", 1,
        [], prohibited, [new ProductInfo(Guid.NewGuid(), "Pistachio latte", 14m, "ILS")], [], [], ["instagram"], 2, null, null, null);

    private static CompetitorFacts Facts() => new(
        1, 12, "engagement rate (interactions / followers)", 0.04m, 3.5m, 0.17m,
        [new PatternGroup("Reel", 6, 0.07m, 1.75m), new PatternGroup("SingleImage", 6, 0.01m, 0.25m)],
        [new PatternGroup("Question", 4, 0.08m, 2m)],
        [new PatternGroup("18-22", 6, 0.07m, 1.75m)],
        [new PatternGroup("Thursday", 2, 0.05m, 1.25m)],
        ["#قهوة"],
        [new SamplePost(ReelPost, "rival_cafe", "Reel", "Question", "Thu 20:00", 0.09m, TheirCaption)]);

    [Fact]
    public async Task Fake_ai_turns_the_strongest_patterns_into_advice_for_our_products()
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));

        var result = await ai.AnalyzeCompetitorsAsync(Context(), Facts(), CancellationToken.None);

        var format = Assert.Single(result.Output, i => i.Type == InsightType.Format);
        Assert.Contains("Reel", format.Summary);
        Assert.Contains("Pistachio latte", format.ApplyToUs);
        Assert.Equal(new[] { ReelPost }, format.EvidencePostIds);
        Assert.Contains(result.Output, i => i.Type == InsightType.PostingTime);
        Assert.Contains(result.Output, i => i.Type == InsightType.Frequency);
    }

    [Fact]
    public async Task Llm_keeps_only_evidence_ids_it_was_given()
    {
        var answer = $$"""
            {"insights":[{"type":"Format","summary":"الريلز بتجيب 1.75 ضعف التفاعل","applyToUs":"اعمل ريل عن لاتيه الفستق","confidence":0.8,
              "evidencePostIds":["{{ReelPost}}","{{Guid.NewGuid()}}"]}]}
            """;
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var result = await ai.AnalyzeCompetitorsAsync(Context(), Facts(), CancellationToken.None);

        var insight = Assert.Single(result.Output);
        Assert.Equal(new[] { ReelPost }, insight.EvidencePostIds);
    }

    [Fact]
    public async Task Advice_that_repeats_a_competitor_caption_goes_to_the_next_model()
    {
        var copied = $$"""{"insights":[{"type":"Hook","summary":"الأسئلة بتجيب تفاعل","applyToUs":"{{TheirCaption}}","confidence":0.7,"evidencePostIds":[]}]}""";
        var own = """{"insights":[{"type":"Hook","summary":"الأسئلة بتجيب ضعفين التفاعل","applyToUs":"ابدأ بوست لاتيه الفستق بسؤال عن طعمهم المفضل","confidence":0.7,"evidencePostIds":[]}]}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Returns(copied)),
            new StubChatModel("Groq", "b", StubChatModel.Returns(own))));

        var result = await ai.AnalyzeCompetitorsAsync(Context(), Facts(), CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Contains("لاتيه الفستق", result.Output[0].ApplyToUs);
    }

    [Fact]
    public async Task Recommendations_cite_only_insights_from_the_context()
    {
        var insight = new InsightInfo(Guid.NewGuid(), InsightType.PostingTime, "Best between 18-22", "Publish at 20:00");
        var context = Context() with { Insights = [insight] };
        var answer = $$"""
            {"recommendations":[{"actionType":"Reschedule","platform":"instagram","details":{"moveTo":"20:00"},
              "rationale":"المسا أحسن وقت","confidence":0.7,"insightIds":["{{insight.Id}}","{{Guid.NewGuid()}}"]}]}
            """;
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));
        var now = DateTimeOffset.UtcNow;

        var result = await ai.RecommendAsync(context, now, now.AddDays(7), CancellationToken.None);

        Assert.Equal(new[] { insight.Id }, Assert.Single(result.Output).InsightIds);
    }

    [Fact]
    public async Task The_prompt_tells_the_model_what_was_learned_and_not_to_copy()
    {
        var model = new StubChatModel("Gemini", "a", StubChatModel.Returns("""{"recommendations":[]}"""));
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(model));
        var context = Context() with { Insights = [new InsightInfo(Guid.NewGuid(), InsightType.Format, "Reels get 2x", "Make a reel")] };
        var now = DateTimeOffset.UtcNow;

        await Assert.ThrowsAnyAsync<Exception>(() => ai.RecommendAsync(context, now, now.AddDays(7), CancellationToken.None));

        Assert.Contains("Reels get 2x", model.LastRequest!.User);
        Assert.Contains("never copy", model.LastRequest.System);
    }
}
