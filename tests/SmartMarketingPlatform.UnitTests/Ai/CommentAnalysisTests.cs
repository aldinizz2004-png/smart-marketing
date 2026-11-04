using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class CommentAnalysisTests
{
    private static MarketingContext Context(params string[] prohibited) => new(
        Guid.NewGuid(), "Qahwet Al-Hara", "Cafe", "Nablus", "Asia/Hebron", "ILS", "Palestinian Arabic", "friendly", 1,
        [], prohibited, [new ProductInfo(Guid.NewGuid(), "Pistachio latte", 14m, "ILS")], [], [], ["instagram"], 2, null, null, null);

    private static CommentInput Comment(string body, string author = "Lina Haddad") =>
        new(Guid.NewGuid(), "instagram", author, body, "New pistachio latte!");

    [Theory]
    [InlineData("بجنن! أطيب لاتيه شربته 😍", CommentCategory.Praise)]
    [InlineData("بكم الحجم الكبير؟", CommentCategory.Question)]
    [InlineData("وصل الطلب بارد ومتأخر", CommentCategory.Complaint)]
    [InlineData("بدي اطلب 3 للمكتب", CommentCategory.PurchaseIntent)]
    [InlineData("free followers https://spam.example", CommentCategory.Spam)]
    [InlineData("👍", CommentCategory.Other)]
    public async Task Fake_ai_classifies_common_comments(string body, CommentCategory expected)
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));

        var result = await ai.AnalyzeCommentsAsync(Context(), [Comment(body)], CancellationToken.None);

        Assert.Equal(expected, result.Output[0].Category);
    }

    [Fact]
    public async Task Fake_ai_suggests_a_reply_only_when_one_is_needed()
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));
        var complaint = Comment("القهوة باردة");
        var spam = Comment("follow me https://x.example");

        var result = await ai.AnalyzeCommentsAsync(Context(), [complaint, spam], CancellationToken.None);

        Assert.True(result.Output[0].NeedsReply);
        Assert.Contains("Lina", result.Output[0].SuggestedReply);
        Assert.False(result.Output[1].NeedsReply);
        Assert.Null(result.Output[1].SuggestedReply);
    }

    [Fact]
    public async Task Llm_answers_for_unknown_comment_ids_are_dropped()
    {
        var known = Comment("بكم اللاتيه؟");
        var answer = $$"""
            {"comments":[
              {"id":"{{known.Id}}","category":"Question","needsReply":true,"suggestedReply":"اللاتيه بـ 14 شيكل"},
              {"id":"{{Guid.NewGuid()}}","category":"Praise","needsReply":true,"suggestedReply":"شكراً"}
            ]}
            """;
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var result = await ai.AnalyzeCommentsAsync(Context(), [known], CancellationToken.None);

        var analysis = Assert.Single(result.Output);
        Assert.Equal(known.Id, analysis.CommentId);
        Assert.Equal("اللاتيه بـ 14 شيكل", analysis.SuggestedReply);
    }

    [Fact]
    public async Task Llm_reply_marked_as_not_needed_is_not_kept()
    {
        var comment = Comment("@friend شوف هاد");
        var answer = $$"""{"comments":[{"id":"{{comment.Id}}","category":"Other","needsReply":false,"suggestedReply":"أهلين"}]}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var result = await ai.AnalyzeCommentsAsync(Context(), [comment], CancellationToken.None);

        Assert.False(result.Output[0].NeedsReply);
        Assert.Null(result.Output[0].SuggestedReply);
    }

    [Fact]
    public async Task A_suggested_reply_with_a_prohibited_word_goes_to_the_next_model()
    {
        var comment = Comment("بكم؟");
        var banned = $$"""{"comments":[{"id":"{{comment.Id}}","category":"Question","needsReply":true,"suggestedReply":"أرخص سعر عنا، cheap!"}]}""";
        var clean = $$"""{"comments":[{"id":"{{comment.Id}}","category":"Question","needsReply":true,"suggestedReply":"14 شيكل بس"}]}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Returns(banned)),
            new StubChatModel("Groq", "b", StubChatModel.Returns(clean))));

        var result = await ai.AnalyzeCommentsAsync(Context("cheap"), [comment], CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Equal("14 شيكل بس", result.Output[0].SuggestedReply);
    }
}
