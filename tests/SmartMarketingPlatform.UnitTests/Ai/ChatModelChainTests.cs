using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class ChatModelChainTests
{
    private static readonly ChatRequest Request = new("system", "user");

    internal static ChatModelChain Chain(params StubChatModel[] models) => new(
        models.Select(m => ((IChatModel)m, new ChainEntry { Provider = m.Provider, Model = m.Model, InputCostPer1MTokensUsd = 1m, OutputCostPer1MTokensUsd = 2m })).ToList(),
        TimeProvider.System,
        NullLogger<ChatModelChain>.Instance);

    private static JsonObject ParseObject(string text) => JsonNode.Parse(text)!.AsObject();

    [Fact]
    public async Task Uses_the_first_model_when_it_answers()
    {
        var first = new StubChatModel("Gemini", "a", StubChatModel.Returns("""{"ok":1}"""));
        var second = new StubChatModel("Groq", "b", StubChatModel.Returns("""{"ok":2}"""));

        var result = await Chain(first, second).RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal(1, (int)result.Output["ok"]!);
        Assert.Equal("Gemini/a", result.ModelName);
        Assert.Empty(result.FailedAttempts);
        Assert.Equal(0, second.Calls);
    }

    [Theory]
    [InlineData("rate_limited")]
    [InlineData("unavailable")]
    [InlineData("timeout")]
    [InlineData("auth_failed")]
    public async Task Falls_back_to_the_next_model_when_a_model_fails(string code)
    {
        var first = new StubChatModel("Gemini", "a", StubChatModel.Fails(code));
        var second = new StubChatModel("Groq", "b", StubChatModel.Returns("""{"ok":2}"""));

        var result = await Chain(first, second).RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Equal([$"Gemini/a: {code}"], result.FailedAttempts);
    }

    [Fact]
    public async Task Falls_back_when_the_answer_is_not_json()
    {
        var first = new StubChatModel("Gemini", "a", StubChatModel.Returns("Sure! Here is your plan."));
        var second = new StubChatModel("Groq", "b", StubChatModel.Returns("""{"ok":2}"""));

        var result = await Chain(first, second).RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal("Groq/b", result.ModelName);
        Assert.Equal(["Gemini/a: invalid_output"], result.FailedAttempts);
    }

    [Fact]
    public async Task Accepts_json_wrapped_in_a_markdown_code_fence()
    {
        var model = new StubChatModel("Groq", "b", StubChatModel.Returns("```json\n{\"ok\":3}\n```"));

        var result = await Chain(model).RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal(3, (int)result.Output["ok"]!);
    }

    [Fact]
    public async Task Throws_when_every_model_fails()
    {
        var chain = Chain(
            new StubChatModel("Gemini", "a", StubChatModel.Fails("rate_limited")),
            new StubChatModel("Groq", "b", StubChatModel.Fails("unavailable")));

        var ex = await Assert.ThrowsAsync<AiProviderException>(() => chain.RunAsync(Request, ParseObject, CancellationToken.None));

        Assert.Equal("all_models_failed", ex.Code);
        Assert.Contains("Gemini/a: rate_limited", ex.Message);
        Assert.Contains("Groq/b: unavailable", ex.Message);
    }

    [Fact]
    public async Task A_rate_limited_model_is_tried_last_on_the_next_request()
    {
        var first = new StubChatModel("Gemini", "a", StubChatModel.Fails("rate_limited"), StubChatModel.Returns("""{"ok":1}"""));
        var second = new StubChatModel("Groq", "b", StubChatModel.Returns("""{"ok":2}"""));
        var chain = Chain(first, second);

        await chain.RunAsync(Request, ParseObject, CancellationToken.None);
        var next = await chain.RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal("Groq/b", next.ModelName);
        Assert.Equal(1, first.Calls);
    }

    [Fact]
    public async Task Calculates_cost_from_the_model_that_answered()
    {
        var model = new StubChatModel("Groq", "b", StubChatModel.Returns("""{"ok":1}"""));

        var result = await Chain(model).RunAsync(Request, ParseObject, CancellationToken.None);

        Assert.Equal(100 / 1_000_000m * 1m + 50 / 1_000_000m * 2m, result.CostUsd);
    }
}
