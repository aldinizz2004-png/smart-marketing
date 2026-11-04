using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Common.Ai.Llm;

// Sends one JSON request to a provider and turns every failure into an AiProviderException
// with a stable code, so the fallback chain can decide what to do without knowing the provider.
internal static class ProviderHttp
{
    public static async Task<JsonObject> PostJsonAsync(
        HttpClient http, HttpRequestMessage request, JsonObject body, string provider, CancellationToken cancellationToken)
    {
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiProviderException("timeout", $"{provider} did not answer in time.");
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderException("unavailable", $"{provider} could not be reached: {ex.Message}");
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new AiProviderException(CodeFor(response.StatusCode), $"{provider} returned {(int)response.StatusCode}: {Shorten(text)}")
                {
                    RetryAfter = response.Headers.RetryAfter?.Delta
                };

            try
            {
                return JsonNode.Parse(text)?.AsObject() ?? throw new AiProviderException("empty_output", $"{provider} returned an empty body.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new AiProviderException("invalid_output", $"{provider} returned a body that is not JSON.");
            }
        }
    }

    private static string CodeFor(HttpStatusCode status) => (int)status switch
    {
        429 => "rate_limited",
        401 or 403 => "auth_failed",
        400 or 404 or 422 => "bad_request",
        408 or 504 => "timeout",
        >= 500 => "unavailable",
        _ => "provider_error"
    };

    // Error bodies can be long; keep enough to understand the problem in AiExecution.ErrorMessage.
    private static string Shorten(string text) => text.Length <= 500 ? text : text[..500];
}
