using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartMarketingPlatform.Common.Ai.Llm;

// The ordered list of models from configuration (Ai:Chain), with automatic fallback:
// a model that fails (quota, overload, timeout, unusable answer) is skipped and the next one is
// asked with the same prompt. A model that hit its quota or was overloaded is put on a short
// cooldown, so the next requests go straight to a model that works instead of waiting again.
public sealed class ChatModelChain
{
    private readonly IReadOnlyList<(IChatModel Model, ChainEntry Entry)> models;
    private readonly ConcurrentDictionary<string, DateTimeOffset> cooldownUntil = new();
    private readonly TimeProvider clock;
    private readonly ILogger<ChatModelChain> logger;

    public ChatModelChain(IHttpClientFactory httpFactory, IOptions<AiOptions> options, TimeProvider clock, ILogger<ChatModelChain> logger)
        : this(CreateModels(httpFactory, options.Value, logger), clock, logger)
    {
    }

    internal ChatModelChain(IReadOnlyList<(IChatModel Model, ChainEntry Entry)> models, TimeProvider clock, ILogger<ChatModelChain> logger)
    {
        if (models.Count == 0)
            throw new InvalidOperationException(
                "Ai:Provider is 'Llm' but no model in Ai:Chain has an API key. Set Ai:Gemini:ApiKey / Ai:Groq:ApiKey (user-secrets or environment).");
        this.models = models;
        this.clock = clock;
        this.logger = logger;
    }

    private static List<(IChatModel Model, ChainEntry Entry)> CreateModels(IHttpClientFactory httpFactory, AiOptions o, ILogger logger)
    {
        var list = new List<(IChatModel, ChainEntry)>();

        foreach (var entry in o.Chain)
        {
            IChatModel? model = entry.Provider.ToLowerInvariant() switch
            {
                "gemini" when !string.IsNullOrWhiteSpace(o.Gemini.ApiKey)
                    => new GeminiChatModel(httpFactory.CreateClient(HttpClientNames.Gemini), o.Gemini.ApiKey!, entry.Model),
                "groq" when !string.IsNullOrWhiteSpace(o.Groq.ApiKey)
                    => new OpenAiCompatibleChatModel(httpFactory.CreateClient(HttpClientNames.Groq), "Groq", o.Groq.ApiKey!, entry.Model),
                _ => null
            };
            if (model is null)
                logger.LogWarning("AI chain entry {Provider}/{Model} skipped: unknown provider or missing API key.", entry.Provider, entry.Model);
            else
                list.Add((model, entry));
        }
        return list;
    }

    // "gemini-3.6-flash > gemini-3.5-flash-lite > ..." (what AiExecution.ModelName shows when every model failed).
    public string Description => string.Join(" > ", models.Select(m => m.Model.Model));

    public async Task<ChainResult<T>> RunAsync<T>(ChatRequest request, Func<string, T> parse, CancellationToken cancellationToken)
    {
        var failures = new List<string>();
        var now = clock.GetUtcNow();

        // Models on cooldown are tried last rather than never: if every model is cooling down, still try.
        var ordered = models.OrderBy(m => cooldownUntil.TryGetValue(Key(m.Model), out var until) && until > now).ToList();

        foreach (var (model, entry) in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var completion = await model.CompleteJsonAsync(request, cancellationToken);
                T output;
                try
                {
                    output = parse(StripCodeFence(completion.Text));
                }
                catch (JsonException ex)
                {
                    throw new AiProviderException("invalid_output", $"The answer is not the expected JSON: {ex.Message}");
                }

                cooldownUntil.TryRemove(Key(model), out _);
                var cost = completion.InputTokens / 1_000_000m * entry.InputCostPer1MTokensUsd
                           + completion.OutputTokens / 1_000_000m * entry.OutputCostPer1MTokensUsd;
                return new ChainResult<T>(output, completion, $"{model.Provider}/{model.Model}", cost, failures);
            }
            catch (AiProviderException ex)
            {
                failures.Add($"{model.Provider}/{model.Model}: {ex.Code}");
                logger.LogWarning("AI model {Provider}/{Model} failed ({Code}): {Message}. Trying the next model.",
                    model.Provider, model.Model, ex.Code, ex.Message);

                if (CooldownFor(ex) is { } pause)
                    cooldownUntil[Key(model)] = clock.GetUtcNow() + pause;
            }
        }

        throw new AiProviderException("all_models_failed", "Every AI model failed: " + string.Join("; ", failures));
    }

    private static TimeSpan? CooldownFor(AiProviderException ex) => ex.Code switch
    {
        "rate_limited" => Clamp(ex.RetryAfter ?? TimeSpan.FromSeconds(60)),
        "unavailable" or "timeout" => Clamp(ex.RetryAfter ?? TimeSpan.FromSeconds(30)),
        "auth_failed" => TimeSpan.FromMinutes(10),   // a wrong key will not fix itself quickly
        _ => null                                     // bad answers are per prompt, not per model
    };

    private static TimeSpan Clamp(TimeSpan value) =>
        value < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : value > TimeSpan.FromMinutes(10) ? TimeSpan.FromMinutes(10) : value;

    private static string Key(IChatModel model) => $"{model.Provider}/{model.Model}";

    // Some models wrap JSON in ```json ... ``` even in JSON mode.
    private static string StripCodeFence(string text)
    {
        var t = text.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal)) return t;
        var firstLineEnd = t.IndexOf('\n');
        var lastFence = t.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd > 0 && lastFence > firstLineEnd ? t[(firstLineEnd + 1)..lastFence].Trim() : t;
    }
}

public sealed record ChainResult<T>(T Output, ChatCompletion Completion, string ModelName, decimal CostUsd, IReadOnlyList<string> FailedAttempts);

public static class HttpClientNames
{
    public const string Gemini = "ai-gemini";
    public const string Groq = "ai-groq";
}
