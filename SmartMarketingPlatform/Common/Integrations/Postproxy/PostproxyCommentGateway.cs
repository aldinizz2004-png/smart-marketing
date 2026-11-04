using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Publishing;

namespace SmartMarketingPlatform.Common.Integrations.Postproxy;

public sealed class PostproxyCommentGateway(PostproxyClient client) : ICommentGateway
{
    public async Task<IReadOnlyList<IncomingComment>> GetCommentsAsync(CommentTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var page = await client.ListCommentsAsync(target.ExternalPostId, target.ExternalAccountId, cancellationToken);
            return Flatten(page).ToList();
        }
        catch (PostproxyException ex)
        {
            throw new CommentGatewayException(ex.Message);
        }
    }

    public async Task ReplyAsync(CommentTarget target, string parentCommentId, string body, CancellationToken cancellationToken)
    {
        try
        {
            await client.ReplyToCommentAsync(target.ExternalPostId, target.ExternalAccountId, parentCommentId, body, cancellationToken);
        }
        catch (PostproxyException ex)
        {
            throw new CommentGatewayException(ex.Message);
        }
    }

    // Replies are nested under their parent comment; they are returned as a flat list.
    public static IEnumerable<IncomingComment> Flatten(JsonNode? node)
    {
        var items = node as JsonArray ?? node?["data"] as JsonArray ?? [];
        foreach (var item in items.OfType<JsonObject>())
        {
            if (Parse(item) is { } comment)
                yield return comment;
            foreach (var reply in Flatten(item["replies"]))
                yield return reply;
        }
    }

    public static IncomingComment? Parse(JsonObject c)
    {
        var id = c["id"]?.ToString();
        var body = c["body"]?.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(body))
            return null;

        var posted = DateTimeOffset.TryParse(c["posted_at"]?.ToString() ?? c["created_at"]?.ToString(), out var at) ? at : DateTimeOffset.UtcNow;
        return new IncomingComment(
            id,
            c["parent_id"]?.ToString() ?? c["parent_external_id"]?.ToString(),
            c["author_external_id"]?.ToString(),
            c["author_name"]?.ToString(),
            c["author_username"]?.ToString(),
            body,
            posted);
    }
}
