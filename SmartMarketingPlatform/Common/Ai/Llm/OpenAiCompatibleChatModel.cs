using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Common.Ai.Llm;

// Any provider that speaks the OpenAI chat-completions format (Groq today; OpenRouter, DeepSeek,
// Mistral ... use the same format, so adding one later is configuration, not code).
public sealed class OpenAiCompatibleChatModel(HttpClient http, string provider, string apiKey, string model) : IChatModel
{
    public string Provider => provider;
    public string Model => model;

    public async Task<ChatCompletion> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = request.System },
                new JsonObject { ["role"] = "user", ["content"] = UserContent(request) }),
            ["temperature"] = request.Temperature,
            ["max_completion_tokens"] = request.MaxOutputTokens,
            ["response_format"] = new JsonObject { ["type"] = "json_object" }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        message.Headers.Authorization = new("Bearer", apiKey);
        var response = await ProviderHttp.PostJsonAsync(http, message, body, $"{provider} {model}", cancellationToken);

        var choice = response["choices"]?.AsArray().FirstOrDefault()
                     ?? throw new AiProviderException("empty_output", $"{provider} returned no choices.");
        var text = choice["message"]?["content"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(text))
            throw new AiProviderException("empty_output", $"{provider} returned no text (finish_reason {choice["finish_reason"]}).");

        var usage = response["usage"];
        return new ChatCompletion(
            text,
            usage?["prompt_tokens"]?.GetValue<int>() ?? 0,
            usage?["completion_tokens"]?.GetValue<int>() ?? 0);
    }

    // Plain text, or text + pictures as data URLs for vision models.
    private static JsonNode UserContent(ChatRequest request)
    {
        if (request.Images.Count == 0) return JsonValue.Create(request.User)!;

        var content = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = request.User });
        foreach (var image in request.Images)
            content.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = $"data:{image.MimeType};base64,{Convert.ToBase64String(image.Data)}" }
            });
        return content;
    }
}
