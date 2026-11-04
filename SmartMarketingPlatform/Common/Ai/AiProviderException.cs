namespace SmartMarketingPlatform.Common.Ai;

// Thrown by an IMarketingAi implementation when the provider fails (timeout, quota, bad answer ...).
public sealed class AiProviderException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;

    // Sent by some providers with a 429/503: how long to leave the model alone.
    public TimeSpan? RetryAfter { get; init; }
}
