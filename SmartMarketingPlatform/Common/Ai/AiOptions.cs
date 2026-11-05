namespace SmartMarketingPlatform.Common.Ai;

// "Ai" section.
//   Provider = "Fake" -> FakeMarketingAi (no network, deterministic; used by tests and demos).
//   Provider = "Llm"  -> LlmMarketingAi: real models, tried in the order of Chain. When a model fails
//                        (quota, overload, timeout, bad JSON, banned words) the next one is used.
// API keys never live in appsettings: user-secrets locally, environment variables in Docker
// (Ai__Gemini__ApiKey, Ai__Groq__ApiKey).
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; init; } = "Fake";

    // Used by the Fake provider only (name recorded on AiExecution + cost estimate).
    public string Model { get; init; } = "fake-marketing-v1";
    public decimal InputCostPer1MTokensUsd { get; init; }
    public decimal OutputCostPer1MTokensUsd { get; init; }

    public int TimeoutSeconds { get; init; } = 45;

    public ProviderCredentials Gemini { get; init; } = new() { BaseUrl = "https://generativelanguage.googleapis.com/v1beta/" };
    public ProviderCredentials Groq { get; init; } = new() { BaseUrl = "https://api.groq.com/openai/v1/" };

    public List<ChainEntry> Chain { get; init; } = [];
}

public sealed class ProviderCredentials
{
    public string? ApiKey { get; init; }
    public string BaseUrl { get; init; } = "";
}

// One step of the fallback chain. Costs are per 1M tokens (0 on a free tier).
public sealed class ChainEntry
{
    public string Provider { get; init; } = "";   // "Gemini" | "Groq"
    public string Model { get; init; } = "";
    public decimal InputCostPer1MTokensUsd { get; init; }
    public decimal OutputCostPer1MTokensUsd { get; init; }
    public bool SupportsImages { get; init; }     // the model can look at pictures
}
