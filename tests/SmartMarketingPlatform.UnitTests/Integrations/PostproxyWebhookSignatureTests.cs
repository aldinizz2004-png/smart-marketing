using SmartMarketingPlatform.Common.Integrations.Postproxy;

namespace SmartMarketingPlatform.UnitTests.Integrations;

public class PostproxyWebhookSignatureTests
{
    private const string Secret = "whsec_test_secret";
    private const string Body = """{"id":"whevt_1","type":"platform_post.published"}""";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    private static string Header(long timestamp, string body = Body, string secret = Secret) =>
        $"t={timestamp},v1={PostproxyWebhookSignature.Compute(secret, timestamp.ToString(), body)}";

    [Fact]
    public void Accepts_a_correctly_signed_request()
    {
        Assert.True(PostproxyWebhookSignature.IsValid(Header(Now.ToUnixTimeSeconds()), Body, Secret, Now, Tolerance));
    }

    [Fact]
    public void Rejects_a_changed_body()
    {
        var header = Header(Now.ToUnixTimeSeconds());

        Assert.False(PostproxyWebhookSignature.IsValid(header, Body.Replace("published", "failed"), Secret, Now, Tolerance));
    }

    [Fact]
    public void Rejects_a_signature_made_with_another_secret()
    {
        var header = Header(Now.ToUnixTimeSeconds(), secret: "whsec_other");

        Assert.False(PostproxyWebhookSignature.IsValid(header, Body, Secret, Now, Tolerance));
    }

    [Fact]
    public void Rejects_an_old_request_to_block_replays()
    {
        var header = Header(Now.AddMinutes(-10).ToUnixTimeSeconds());

        Assert.False(PostproxyWebhookSignature.IsValid(header, Body, Secret, Now, Tolerance));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("v1=abc")]
    [InlineData("t=notanumber,v1=abc")]
    public void Rejects_missing_or_malformed_headers(string? header)
    {
        Assert.False(PostproxyWebhookSignature.IsValid(header, Body, Secret, Now, Tolerance));
    }

    [Fact]
    public void Accepts_any_of_several_signatures_during_secret_rotation()
    {
        var timestamp = Now.ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=deadbeef,v1={PostproxyWebhookSignature.Compute(Secret, timestamp.ToString(), Body)}";

        Assert.True(PostproxyWebhookSignature.IsValid(header, Body, Secret, Now, Tolerance));
    }
}
