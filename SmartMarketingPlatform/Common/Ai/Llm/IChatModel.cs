namespace SmartMarketingPlatform.Common.Ai.Llm;

// One model at one provider that answers a prompt with a JSON text.
// Implementations translate provider errors into AiProviderException with a stable code:
//   rate_limited (429), unavailable (5xx / overloaded), timeout, auth_failed (401/403),
//   bad_request (400/404), blocked (safety filter), empty_output.
public interface IChatModel
{
    string Provider { get; }
    string Model { get; }

    Task<ChatCompletion> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken);
}

public sealed record ChatRequest(string System, string User, double Temperature = 0.7, int MaxOutputTokens = 8192)
{
    // Pictures sent with the prompt. Only chain entries with SupportsImages receive such a request.
    public IReadOnlyList<ChatImage> Images { get; init; } = [];
}

public sealed record ChatImage(string MimeType, byte[] Data);

public sealed record ChatCompletion(string Text, int InputTokens, int OutputTokens);
