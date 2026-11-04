using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Common.Storage;

namespace SmartMarketingPlatform.UnitTests.Integrations;

public class PostproxySocialPublisherTests
{
    private const string ProfileId = "prof_ig_1";

    private static PublishRequest Request(params PublishMedia[] media) =>
        new("instagram", ProfileId, null, "New pistachio latte!", media, "publish:job-1");

    private static (PostproxySocialPublisher Publisher, StubHandler Handler) Create(StubHandler handler, InMemoryStorage? storage = null)
    {
        var client = new PostproxyClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.postproxy.test/") });
        var options = Options.Create(new PostproxyOptions { ApiKey = "test", PublishWaitSeconds = 0, MaxMediaBytes = 1024 });
        var publisher = new PostproxySocialPublisher(client, storage ?? new InMemoryStorage(), options, TimeProvider.System, NullLogger<PostproxySocialPublisher>.Instance);
        return (publisher, handler);
    }

    private static string Post(string status, string? permalink = null, string? error = null) => new JsonObject
    {
        ["id"] = "post_1",
        ["platforms"] = new JsonArray(new JsonObject
        {
            ["profile_id"] = ProfileId,
            ["platform"] = "instagram",
            ["status"] = status,
            ["permalink"] = permalink,
            ["error"] = error
        })
    }.ToJsonString();

    [Fact]
    public async Task Returns_the_post_id_and_permalink_when_published()
    {
        var (publisher, handler) = Create(new StubHandler(HttpStatusCode.Created, Post("published", "https://www.instagram.com/p/abc/")));

        var result = await publisher.PublishAsync(Request(), CancellationToken.None);

        Assert.Equal(new PublishResult("post_1", "https://www.instagram.com/p/abc/"), result);
        Assert.Equal("publish:job-1", handler.Requests[0].IdempotencyKey);
        Assert.Equal(ProfileId, JsonNode.Parse(handler.Requests[0].Body!)!["profiles"]![0]!.ToString());
    }

    [Fact]
    public async Task Returns_without_a_url_when_the_platform_is_still_processing()
    {
        var (publisher, _) = Create(new StubHandler(HttpStatusCode.Created, Post("processing")));

        var result = await publisher.PublishAsync(Request(), CancellationToken.None);

        Assert.Equal(new PublishResult("post_1", null), result);
    }

    [Fact]
    public async Task A_platform_rejection_is_a_permanent_failure()
    {
        var (publisher, _) = Create(new StubHandler(HttpStatusCode.Created, Post("failed", error: "Media aspect ratio not supported")));

        var ex = await Assert.ThrowsAsync<PublishException>(() => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.False(ex.IsTransient);
        Assert.Contains("aspect ratio", ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    public async Task Provider_errors_are_retried_only_when_transient(HttpStatusCode status, bool transient)
    {
        var (publisher, _) = Create(new StubHandler(status, """{"error":"boom"}"""));

        var ex = await Assert.ThrowsAsync<PublishException>(() => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Equal(transient, ex.IsTransient);
    }

    [Fact]
    public async Task Sends_media_as_data_uris()
    {
        var storage = new InMemoryStorage();
        storage.Files["media/1.jpg"] = [0xFF, 0xD8, 0xFF, 0xE0];
        var (publisher, handler) = Create(new StubHandler(HttpStatusCode.Created, Post("published")), storage);

        await publisher.PublishAsync(Request(new PublishMedia("media/1.jpg", "image/jpeg")), CancellationToken.None);

        var media = JsonNode.Parse(handler.Requests[0].Body!)!["media"]![0]!.ToString();
        Assert.Equal("data:image/jpeg;base64,/9j/4A==", media);
    }

    [Fact]
    public async Task Rejects_media_larger_than_the_limit_without_calling_the_provider()
    {
        var storage = new InMemoryStorage();
        storage.Files["media/big.jpg"] = new byte[2048];
        var (publisher, handler) = Create(new StubHandler(HttpStatusCode.Created, Post("published")), storage);

        var ex = await Assert.ThrowsAsync<PublishException>(() =>
            publisher.PublishAsync(Request(new PublishMedia("media/big.jpg", "image/jpeg")), CancellationToken.None));

        Assert.False(ex.IsTransient);
        Assert.Empty(handler.Requests);
    }

    internal sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<(string? IdempotencyKey, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.First() : null;
            Requests.Add((key, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    internal sealed class InMemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public Task SaveAsync(string objectKey, Stream content, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Files[objectKey]));
        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
