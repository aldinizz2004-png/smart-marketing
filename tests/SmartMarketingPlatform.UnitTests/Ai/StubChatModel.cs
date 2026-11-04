using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;

namespace SmartMarketingPlatform.UnitTests.Ai;

internal sealed class StubChatModel(string provider, string model, params Func<ChatRequest, string>[] answers) : IChatModel
{
    public string Provider => provider;
    public string Model => model;
    public int Calls { get; private set; }
    public ChatRequest? LastRequest { get; private set; }

    public Task<ChatCompletion> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var answer = answers[Math.Min(Calls, answers.Length - 1)];
        Calls++;
        return Task.FromResult(new ChatCompletion(answer(request), 100, 50));
    }

    public static Func<ChatRequest, string> Returns(string json) => _ => json;

    public static Func<ChatRequest, string> Fails(string code) => _ => throw new AiProviderException(code, $"stub failure: {code}");
}
