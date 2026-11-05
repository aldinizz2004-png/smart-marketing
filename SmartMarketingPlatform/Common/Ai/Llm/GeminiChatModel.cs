using System.Text;
using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Common.Ai.Llm;

// Google Gemini (AI Studio API): POST models/{model}:generateContent, key in the x-goog-api-key header.
// responseMimeType = application/json makes the model answer with JSON only.
public sealed class GeminiChatModel(HttpClient http, string apiKey, string model) : IChatModel
{
    public string Provider => "Gemini";
    public string Model => model;

    public async Task<ChatCompletion> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var parts = new JsonArray();
        foreach (var image in request.Images)
            parts.Add(new JsonObject
            {
                ["inline_data"] = new JsonObject { ["mime_type"] = image.MimeType, ["data"] = Convert.ToBase64String(image.Data) }
            });
        parts.Add(new JsonObject { ["text"] = request.User });

        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = request.System }) },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = parts
            }),
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["temperature"] = request.Temperature,
                ["maxOutputTokens"] = request.MaxOutputTokens
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"models/{Uri.EscapeDataString(model)}:generateContent");
        message.Headers.Add("x-goog-api-key", apiKey);
        var response = await ProviderHttp.PostJsonAsync(http, message, body, $"Gemini {model}", cancellationToken);

        if (response["promptFeedback"]?["blockReason"] is { } blocked)
            throw new AiProviderException("blocked", $"Gemini blocked the prompt ({blocked}).");

        var candidate = response["candidates"]?.AsArray().FirstOrDefault()
                        ?? throw new AiProviderException("empty_output", "Gemini returned no candidates.");
        var finish = candidate["finishReason"]?.GetValue<string>();
        if (finish is "SAFETY" or "PROHIBITED_CONTENT" or "BLOCKLIST")
            throw new AiProviderException("blocked", $"Gemini stopped the answer ({finish}).");

        // Thinking models can return "thought" parts first; only the answer parts are kept.
        var text = new StringBuilder();
        foreach (var part in candidate["content"]?["parts"]?.AsArray() ?? [])
            if (part?["thought"]?.GetValue<bool>() != true && part?["text"] is { } t)
                text.Append(t.GetValue<string>());

        if (text.Length == 0)
            throw new AiProviderException("empty_output", $"Gemini returned no text (finishReason {finish}).");

        var usage = response["usageMetadata"];
        return new ChatCompletion(
            text.ToString(),
            usage?["promptTokenCount"]?.GetValue<int>() ?? 0,
            (usage?["candidatesTokenCount"]?.GetValue<int>() ?? 0) + (usage?["thoughtsTokenCount"]?.GetValue<int>() ?? 0));
    }
}
