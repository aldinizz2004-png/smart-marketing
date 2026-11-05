using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.UnitTests.Ai;

public class ImageDescriptionTests
{
    private static readonly ProductInfo Latte = new(Guid.NewGuid(), "لاتيه الفستق", 14m, "ILS");

    private static MarketingContext Context() => new(
        Guid.NewGuid(), "Qahwet Al-Hara", "Cafe", "Nablus", "Asia/Hebron", "ILS", "Palestinian Arabic", "friendly", 1,
        [], [], [Latte], [], [], ["instagram"], 2, null, null, null);

    private static ImageInput Photo(int width, int height) => new("image/png", [1, 2, 3], width, height);

    // Only the entries marked SupportsImages may receive a picture.
    private static ChatModelChain Chain(params (StubChatModel Model, bool Vision)[] models) => new(
        models.Select(m => ((IChatModel)m.Model, new ChainEntry { Provider = m.Model.Provider, Model = m.Model.Model, SupportsImages = m.Vision })).ToList(),
        TimeProvider.System,
        NullLogger<ChatModelChain>.Instance);

    [Fact]
    public async Task Fake_ai_flags_a_small_photo()
    {
        var ai = new FakeMarketingAi(Options.Create(new AiOptions()));

        var small = await ai.DescribeImageAsync(Context(), Photo(640, 480), Latte, CancellationToken.None);
        var good = await ai.DescribeImageAsync(Context(), Photo(1080, 1080), Latte, CancellationToken.None);

        Assert.Contains(ImageQualityIssue.LowResolution, small.Output.Issues);
        Assert.NotNull(small.Output.Advice);
        Assert.Empty(good.Output.Issues);
        Assert.Contains("لاتيه الفستق", good.Output.Description);
    }

    [Fact]
    public async Task Text_only_models_are_skipped_for_a_picture()
    {
        var answer = """{"description":"كوب لاتيه أخضر فاتح على طاولة خشب","showsProduct":true,"issues":[],"advice":null}""";
        var textOnly = new StubChatModel("Groq", "text", StubChatModel.Returns(answer));
        var vision = new StubChatModel("Gemini", "vision", StubChatModel.Returns(answer));
        var ai = new LlmMarketingAi(Chain((textOnly, false), (vision, true)));

        var result = await ai.DescribeImageAsync(Context(), Photo(1080, 1080), Latte, CancellationToken.None);

        Assert.Equal("Gemini/vision", result.ModelName);
        Assert.Equal(0, textOnly.Calls);
        Assert.Single(vision.LastRequest!.Images);
    }

    [Fact]
    public async Task Without_a_vision_model_the_call_fails_clearly()
    {
        var ai = new LlmMarketingAi(Chain((new StubChatModel("Groq", "text", StubChatModel.Returns("{}")), false)));

        var error = await Assert.ThrowsAsync<AiProviderException>(() =>
            ai.DescribeImageAsync(Context(), Photo(1080, 1080), Latte, CancellationToken.None));

        Assert.Equal("no_vision_model", error.Code);
    }

    [Theory]
    [InlineData(1080, 1080, false)]
    [InlineData(500, 900, true)]
    public async Task Resolution_comes_from_the_measurement_not_from_the_model(int width, int height, bool lowResolution)
    {
        var answer = """{"description":"كوب لاتيه","showsProduct":true,"issues":["LowResolution","Dark","NotAnIssue"],"advice":"صوّر قرب الشباك"}""";
        var ai = new LlmMarketingAi(Chain((new StubChatModel("Gemini", "vision", StubChatModel.Returns(answer)), true)));

        var result = await ai.DescribeImageAsync(Context(), Photo(width, height), Latte, CancellationToken.None);

        Assert.Equal(lowResolution, result.Output.Issues.Contains(ImageQualityIssue.LowResolution));
        Assert.Contains(ImageQualityIssue.Dark, result.Output.Issues);
        Assert.Equal(lowResolution ? 2 : 1, result.Output.Issues.Count);
    }

    private sealed class CapturingHandler(string answer) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer) };
        }
    }

    [Fact]
    public async Task Gemini_receives_the_picture_as_inline_data()
    {
        var handler = new CapturingHandler("""{"candidates":[{"content":{"parts":[{"text":"{}"}]},"finishReason":"STOP"}]}""");
        var gemini = new GeminiChatModel(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") }, "key", "gemini-test");

        await gemini.CompleteJsonAsync(new ChatRequest("system", "describe") { Images = [new ChatImage("image/png", [1, 2, 3])] }, CancellationToken.None);

        var parts = JsonNode.Parse(handler.Body!)!["contents"]![0]!["parts"]!.AsArray();
        Assert.Equal("image/png", parts[0]!["inline_data"]!["mime_type"]!.ToString());
        Assert.Equal(Convert.ToBase64String([1, 2, 3]), parts[0]!["inline_data"]!["data"]!.ToString());
        Assert.Equal("describe", parts[1]!["text"]!.ToString());
    }

    [Fact]
    public async Task OpenAi_compatible_models_receive_the_picture_as_a_data_url()
    {
        var handler = new CapturingHandler("""{"choices":[{"message":{"content":"{}"}}]}""");
        var model = new OpenAiCompatibleChatModel(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") }, "Groq", "key", "vision-test");

        await model.CompleteJsonAsync(new ChatRequest("system", "describe") { Images = [new ChatImage("image/jpeg", [9])] }, CancellationToken.None);

        var content = JsonNode.Parse(handler.Body!)!["messages"]![1]!["content"]!.AsArray();
        Assert.Equal("data:image/jpeg;base64,CQ==", content[1]!["image_url"]!["url"]!.ToString());
    }
}
