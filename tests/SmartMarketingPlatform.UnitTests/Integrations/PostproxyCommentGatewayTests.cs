using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Integrations.Postproxy;

namespace SmartMarketingPlatform.UnitTests.Integrations;

public class PostproxyCommentGatewayTests
{
    [Fact]
    public void Flattens_nested_replies_into_one_list()
    {
        var page = JsonNode.Parse("""
            {"total":1,"page":1,"per_page":100,"data":[
              {"id":"cmt_1","body":"Looks delicious!","author_name":"Lina","author_username":"lina.h","posted_at":"2026-11-04T10:00:00Z",
               "replies":[{"id":"cmt_2","parent_id":"cmt_1","body":"Thank you Lina!","posted_at":"2026-11-04T10:05:00Z","replies":[]}]}
            ]}
            """);

        var comments = PostproxyCommentGateway.Flatten(page).ToList();

        Assert.Equal(["cmt_1", "cmt_2"], comments.Select(c => c.ExternalCommentId));
        Assert.Null(comments[0].ParentExternalId);
        Assert.Equal("cmt_1", comments[1].ParentExternalId);
        Assert.Equal("lina.h", comments[0].AuthorUsername);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero), comments[0].PostedAt);
    }

    [Fact]
    public void Skips_comments_without_text()
    {
        var page = JsonNode.Parse("""[{"id":"cmt_1","body":""},{"id":"cmt_2","body":"ok"}]""");

        var comments = PostproxyCommentGateway.Flatten(page).ToList();

        Assert.Equal("cmt_2", Assert.Single(comments).ExternalCommentId);
    }

    [Fact]
    public void Parses_a_comment_created_webhook_object()
    {
        var data = JsonNode.Parse("""
            {"id":"cmt_9","post_id":"post_1","profile_id":"prof_1","external_id":"1789","parent_external_id":null,
             "body":"بكم الحجم الكبير؟","author_name":"Omar","author_username":"omar_s","author_external_id":"555","posted_at":"2026-11-04T12:00:00Z"}
            """)!.AsObject();

        var comment = PostproxyCommentGateway.Parse(data);

        Assert.NotNull(comment);
        Assert.Equal("cmt_9", comment.ExternalCommentId);
        Assert.Equal("555", comment.AuthorExternalId);
        Assert.Equal("بكم الحجم الكبير؟", comment.Body);
    }
}
