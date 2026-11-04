using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

public sealed class PostproxyClient(HttpClient http)
{
    public async Task<string> CreateProfileGroupAsync(string name, CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["profile_group"] = new JsonObject { ["name"] = name } };
        var group = await SendAsync(HttpMethod.Post, "api/profile_groups", body, null, cancellationToken);
        return Required(group, "id");
    }

    public async Task<string> InitializeConnectionAsync(string profileGroupId, string platform, string redirectUrl, CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["platform"] = platform, ["redirect_url"] = redirectUrl };
        if (platform == "instagram")
            body["onboarding"] = "facebook";

        var result = await SendAsync(HttpMethod.Post, $"api/profile_groups/{Uri.EscapeDataString(profileGroupId)}/initialize_connection", body, null, cancellationToken);
        return Required(result, "url");
    }

    public async Task<PostproxyProfile> GetProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        var p = await SendAsync(HttpMethod.Get, $"api/profiles/{Uri.EscapeDataString(profileId)}", null, null, cancellationToken);
        return new PostproxyProfile(
            Required(p, "id"),
            Required(p, "platform"),
            p["profile_group_id"]?.ToString(),
            p["username"]?.ToString(),
            p["name"]?.ToString(),
            p["status"]?.ToString());
    }

    public Task<JsonObject> CreatePostAsync(JsonObject body, string idempotencyKey, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "api/posts", body, idempotencyKey, cancellationToken);

    public Task<JsonObject> GetPostAsync(string postId, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, $"api/posts/{Uri.EscapeDataString(postId)}", null, null, cancellationToken);

    public Task<JsonObject> GetStatsAsync(string postId, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, $"api/posts/stats?post_ids={Uri.EscapeDataString(postId)}", null, null, cancellationToken);

    private async Task<JsonObject> SendAsync(HttpMethod method, string path, JsonObject? body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PostproxyException(HttpStatusCode.RequestTimeout, "Postproxy did not answer in time.");
        }
        catch (HttpRequestException ex)
        {
            throw new PostproxyException(HttpStatusCode.ServiceUnavailable, $"Postproxy could not be reached: {ex.Message}");
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new PostproxyException(response.StatusCode, $"Postproxy returned {(int)response.StatusCode}: {(text.Length > 500 ? text[..500] : text)}");

            var root = string.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text) as JsonObject ?? new JsonObject();
            return root["data"] is JsonObject data && root["id"] is null ? data : root;
        }
    }

    private static string Required(JsonObject json, string name) =>
        json[name]?.ToString() is { Length: > 0 } value
            ? value
            : throw new PostproxyException(HttpStatusCode.BadGateway, $"Postproxy response has no '{name}'.");
}

public sealed record PostproxyProfile(string Id, string Platform, string? ProfileGroupId, string? Username, string? Name, string? Status);

public sealed class PostproxyException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public bool IsTransient => StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout or HttpStatusCode.Conflict
                               || (int)StatusCode >= 500;
}
