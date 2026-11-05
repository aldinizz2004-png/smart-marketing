using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class CustomerReplyTests
{
    private static MarketingContext Context(params string[] prohibited) => new(
        Guid.NewGuid(), "Qahwet Al-Hara", "Cafe", "Nablus", "Asia/Hebron", "ILS", "Palestinian Arabic", "friendly", 1,
        [], prohibited, [new ProductInfo(Guid.NewGuid(), "لاتيه الفستق", 14m, "ILS")], [], [], ["instagram"], 2, null, null, null);

    private static ChatThread Thread(params string[] customerMessages) =>
        new("Lina Haddad", customerMessages.Select(m => new ChatLine(true, m, "Thu 20:00")).ToList());

    private static readonly FakeMarketingAi Fake = new(Options.Create(new AiOptions()));

    [Fact]
    public async Task A_price_question_about_a_known_product_is_answered_alone()
    {
        var result = await Fake.ReplyToCustomerAsync(Context(), Thread("مرحبا", "قديش سعر لاتيه الفستق؟"), CancellationToken.None);

        Assert.Equal(ConversationIntent.Question, result.Output.Intent);
        Assert.False(result.Output.NeedsOwner);
        Assert.Contains("14", result.Output.Reply);
    }

    [Fact]
    public async Task A_word_that_only_contains_a_price_word_is_not_a_price_question()
    {
        var result = await Fake.ReplyToCustomerAsync(Context(), Thread("شو عندكم اليوم"), CancellationToken.None);

        Assert.NotEqual(ConversationIntent.Question, result.Output.Intent);
    }

    [Theory]
    [InlineData("بدي اطلب ٢ لاتيه توصيل", ConversationIntent.Order)]
    [InlineData("القهوة وصلت باردة", ConversationIntent.Complaint)]
    [InlineData("قديش سعر الكنافة؟", ConversationIntent.Question)]
    public async Task Orders_complaints_and_unknown_products_go_to_the_owner(string message, ConversationIntent intent)
    {
        var result = await Fake.ReplyToCustomerAsync(Context(), Thread(message), CancellationToken.None);

        Assert.Equal(intent, result.Output.Intent);
        Assert.True(result.Output.NeedsOwner);
        Assert.NotNull(result.Output.HandoffReason);
    }

    [Fact]
    public async Task Llm_reply_with_a_number_that_is_not_a_known_price_goes_to_the_owner()
    {
        var answer = """{"intent":"Question","reply":"لاتيه الفستق بـ 12 شيكل بس!","needsOwner":false,"handoffReason":null,"confidence":0.9}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var result = await ai.ReplyToCustomerAsync(Context(), Thread("قديش لاتيه الفستق؟"), CancellationToken.None);

        Assert.True(result.Output.NeedsOwner);
        Assert.Contains("12", result.Output.HandoffReason);
    }

    [Fact]
    public async Task Llm_reply_with_the_real_price_can_be_sent_alone()
    {
        var answer = """{"intent":"Question","reply":"لاتيه الفستق بـ ١٤ شيكل","needsOwner":false,"handoffReason":null,"confidence":0.9}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(new StubChatModel("Gemini", "a", StubChatModel.Returns(answer))));

        var result = await ai.ReplyToCustomerAsync(Context(), Thread("قديش لاتيه الفستق؟"), CancellationToken.None);

        Assert.False(result.Output.NeedsOwner);
        Assert.Equal(0.9m, result.Output.Confidence);
    }

    [Fact]
    public async Task A_reply_with_a_prohibited_word_goes_to_the_next_model()
    {
        var banned = """{"intent":"Question","reply":"عنا أرخص قهوة، رخيص كتير","needsOwner":false,"confidence":0.9}""";
        var clean = """{"intent":"Question","reply":"لاتيه الفستق بـ 14 شيكل","needsOwner":false,"confidence":0.9}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Returns(banned)),
            new StubChatModel("Groq", "b", StubChatModel.Returns(clean))));

        var result = await ai.ReplyToCustomerAsync(Context("رخيص"), Thread("قديش؟"), CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
    }

    [Fact]
    public async Task No_reply_without_a_handoff_is_not_accepted()
    {
        var empty = """{"intent":"Other","reply":null,"needsOwner":false,"confidence":0.5}""";
        var handoff = """{"intent":"Other","reply":null,"needsOwner":true,"handoffReason":"Unclear","confidence":0.3}""";
        var ai = new LlmMarketingAi(ChatModelChainTests.Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Returns(empty)),
            new StubChatModel("Groq", "b", StubChatModel.Returns(handoff))));

        var result = await ai.ReplyToCustomerAsync(Context(), Thread("؟؟"), CancellationToken.None);

        Assert.True(result.Output.NeedsOwner);
        Assert.Equal("Unclear", result.Output.HandoffReason);
    }
}
