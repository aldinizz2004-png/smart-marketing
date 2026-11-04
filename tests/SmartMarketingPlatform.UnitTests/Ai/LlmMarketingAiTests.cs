using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class LlmMarketingAiTests
{
    private static readonly Guid LatteId = Guid.NewGuid();

    private static MarketingContext Context() => new(
        BusinessId: Guid.NewGuid(),
        BusinessName: "Qahwet Al-Hara",
        Category: "Cafe",
        City: "Nablus",
        TimeZoneId: "Asia/Hebron",
        Currency: "ILS",
        Dialect: "Palestinian Arabic",
        Tone: "friendly",
        EmojiLevel: 1,
        PreferredTerms: ["fresh"],
        ProhibitedTerms: ["cheap"],
        Products: [new ProductInfo(LatteId, "Pistachio latte", 14m, "ILS")],
        Offers: [],
        Audiences: [],
        Channels: ["instagram", "facebook"],
        MaxPostsPerDay: 2,
        PublishWindowStart: "10:00",
        PublishWindowEnd: "22:00",
        Campaign: null);

    private static ContentBrief Brief(ContentFormat? format = null) =>
        new("instagram", ContentType.Post, format, HookType.Question, null, null, null, null);

    private const string GoodPost = """
        {"body":"Have you tried our pistachio latte?","format":"Carousel","hookType":"Question","callToAction":"VisitStore","includesOffer":false,"visualIdea":"cup close-up","hashtags":["nablus","coffee time"],"language":"en"}
        """;

    [Fact]
    public async Task Writes_content_from_the_model_answer()
    {
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(GoodPost))));

        var result = await ai.WriteContentAsync(Context(), Brief(), CancellationToken.None);

        Assert.Equal("Have you tried our pistachio latte?", result.Output.Body);
        Assert.Equal(CallToAction.VisitStore, result.Output.CallToAction);
        Assert.Equal("Gemini/a", result.ModelName);
        Assert.Equal(["#nablus", "#coffee_time"], result.Output.CreativeDetails["hashtags"]!.AsArray().Select(t => t!.ToString()));
    }

    [Fact]
    public async Task The_format_requested_by_the_owner_wins_over_the_model_choice()
    {
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(GoodPost))));

        var result = await ai.WriteContentAsync(Context(), Brief(ContentFormat.SingleImage), CancellationToken.None);

        Assert.Equal(ContentFormat.SingleImage, result.Output.Format);
    }

    [Fact]
    public async Task Text_with_a_prohibited_term_is_rejected_and_the_next_model_writes_it()
    {
        var banned = """{"body":"The CHEAP latte in town!","format":"SingleImage"}""";
        var first = new StubChatModel("Gemini", "a", StubChatModel.Returns(banned));
        var second = new StubChatModel("Groq", "b", StubChatModel.Returns(GoodPost));
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(first, second));

        var result = await ai.WriteContentAsync(Context(), Brief(), CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Equal(["Gemini/a: prohibited_terms"], result.FailedAttempts);
        Assert.DoesNotContain("cheap", result.Output.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_prompt_contains_the_business_facts_and_the_banned_words()
    {
        var model = new StubChatModel("Gemini", "a", StubChatModel.Returns(GoodPost));
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(model));

        await ai.WriteContentAsync(Context(), Brief(), CancellationToken.None);

        Assert.Contains("Pistachio latte", model.LastRequest!.User);
        Assert.Contains("\"cheap\"", model.LastRequest.User);
        Assert.Contains("Palestinian Arabic", model.LastRequest.System);
    }

    [Fact]
    public async Task Recommendations_drop_invented_ids_and_unknown_platforms()
    {
        var invented = Guid.NewGuid();
        var answer = $$"""
            {"recommendations":[
              {"actionType":"NewContent","platform":"instagram","contentType":"Reel","productId":"{{LatteId}}","captionIdea":"latte reel","rationale":"reels reach more people","confidence":0.9},
              {"actionType":"NewContent","platform":"snapchat","contentType":"Post","productId":"{{invented}}","captionIdea":"x","rationale":"try a new channel","confidence":3},
              {"actionType":"NotARealAction","platform":"instagram","rationale":"ignored"}
            ]}
            """;
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));
        var now = DateTimeOffset.UtcNow;

        var result = await ai.RecommendAsync(Context(), now, now.AddDays(7), CancellationToken.None);

        Assert.Equal(2, result.Output.Count);
        Assert.Equal(LatteId, result.Output[0].Payload["productId"]!.GetValue<Guid>());
        Assert.Equal("instagram", result.Output[1].Payload["platform"]!.ToString());
        Assert.Null(result.Output[1].Payload["productId"]);
        Assert.Equal(1m, result.Output[1].Confidence);
    }

    [Fact]
    public async Task A_publish_time_outside_the_window_is_dropped()
    {
        var answer = """
            {"recommendations":[{"actionType":"NewContent","platform":"instagram","publishAtLocal":"2001-01-01T19:00","rationale":"old date","confidence":0.5}]}
            """;
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));
        var now = DateTimeOffset.UtcNow;

        var result = await ai.RecommendAsync(Context(), now, now.AddDays(7), CancellationToken.None);

        Assert.Null(result.Output[0].ProposedPublishAt);
    }
}
