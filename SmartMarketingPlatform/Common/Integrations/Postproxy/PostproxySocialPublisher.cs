using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Common.Storage;

namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

// Our worker decides when to publish, so posts are sent for immediate publishing.
// Postproxy publishes asynchronously: we wait a few seconds for the platform result,
// and anything that finishes later arrives through the webhooks.
public sealed class PostproxySocialPublisher(
    PostproxyClient client,
    IFileStorage storage,
    IOptions<PostproxyOptions> options,
    TimeProvider clock,
    ILogger<PostproxySocialPublisher> logger) : ISocialPublisher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public bool RequiresAccessToken => false;

    public async Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["post"] = new JsonObject { ["body"] = request.Body },
            ["profiles"] = new JsonArray(request.ExternalAccountId),
            ["media"] = await MediaAsync(request.Media, cancellationToken)
        };

        try
        {
            var post = await client.CreatePostAsync(body, request.IdempotencyKey, cancellationToken);
            var postId = post["id"]?.ToString() ?? throw new PublishException("Postproxy returned a post without an id.", isTransient: true);

            var deadline = clock.GetUtcNow().AddSeconds(options.Value.PublishWaitSeconds);
            while (true)
            {
                var platform = FindPlatform(post, request.ExternalAccountId);
                switch (platform?["status"]?.ToString())
                {
                    case "published":
                        return new PublishResult(postId, platform!["permalink"]?.ToString());
                    case "failed":
                        throw new PublishException(
                            $"The platform rejected the post: {platform!["error"]?.ToString() ?? "unknown error"}", isTransient: false);
                }

                if (clock.GetUtcNow() >= deadline)
                {
                    logger.LogInformation("Postproxy post {PostId} is still processing; the result will come by webhook.", postId);
                    return new PublishResult(postId, null);
                }

                await Task.Delay(PollInterval, cancellationToken);
                post = await client.GetPostAsync(postId, cancellationToken);
            }
        }
        catch (PostproxyException ex)
        {
            throw new PublishException(ex.Message, ex.IsTransient);
        }
    }

    public static JsonObject? FindPlatform(JsonObject post, string profileId) =>
        (post["platforms"] as JsonArray)?.OfType<JsonObject>()
            .FirstOrDefault(p => p["profile_id"]?.ToString() == profileId);

    private async Task<JsonArray> MediaAsync(IReadOnlyList<PublishMedia> media, CancellationToken cancellationToken)
    {
        var result = new JsonArray();
        long total = 0;
        foreach (var item in media)
        {
            await using var stream = await storage.OpenReadAsync(item.ObjectKey, cancellationToken);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            total += buffer.Length;
            if (total > options.Value.MaxMediaBytes)
                throw new PublishException("The media of this post is too large to send.", isTransient: false);

            result.Add($"data:{item.MimeType};base64,{Convert.ToBase64String(buffer.GetBuffer(), 0, (int)buffer.Length)}");
        }
        return result;
    }
}
