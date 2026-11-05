using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Integrations.WhatsApp;
using SmartMarketingPlatform.Common.Messaging;

namespace SmartMarketingPlatform.UnitTests.Messaging;

public class MetaWhatsAppGatewayTests
{
    private sealed class RecordingHandler(HttpStatusCode status, string answer) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(answer) };
        }
    }

    private static readonly WhatsAppSender Sender = new("PNID1", "token-123");

    private static MetaWhatsAppGateway Gateway(RecordingHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") }, Options.Create(new WhatsAppOptions { ApiVersion = "v23.0" }));

    [Fact]
    public async Task Sends_a_text_from_the_business_number()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"messaging_product":"whatsapp","messages":[{"id":"wamid.OUT"}]}""");

        var id = await Gateway(handler).SendTextAsync(Sender, "970599111222", "أهلين!", CancellationToken.None);

        Assert.Equal("wamid.OUT", id);
        Assert.Equal("https://graph.facebook.com/v23.0/PNID1/messages", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer token-123", handler.Request.Headers.Authorization!.ToString());
        var body = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("970599111222", body["to"]!.ToString());
        Assert.Equal("أهلين!", body["text"]!["body"]!.ToString());
    }

    [Fact]
    public async Task Outside_the_24_hour_window_is_reported_as_such()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"error":{"message":"Re-engagement message","code":131047}}""");

        var error = await Assert.ThrowsAsync<WhatsAppGatewayException>(() =>
            Gateway(handler).SendTextAsync(Sender, "970599111222", "hi", CancellationToken.None));

        Assert.True(error.OutsideWindow);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 190, "auth_failed")]
    [InlineData(HttpStatusCode.BadRequest, 130429, "rate_limited")]
    [InlineData(HttpStatusCode.BadRequest, 131026, "undeliverable")]
    [InlineData(HttpStatusCode.BadGateway, 1, "unavailable")]
    public void Maps_cloud_api_errors(HttpStatusCode status, int code, string expected)
    {
        var error = MetaWhatsAppGateway.ToError(status, $$$"""{"error":{"message":"x","code":{{{code}}}}}""");

        Assert.Equal(expected, error.Code);
    }
}
