using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Integrations.WhatsApp;

namespace SmartMarketingPlatform.UnitTests.Messaging;

public class WhatsAppWebhookTests
{
    private const string Secret = "app-secret";

    private const string Payload = """
        {"object":"whatsapp_business_account","entry":[{"id":"WABA","changes":[{"field":"messages","value":{
          "messaging_product":"whatsapp",
          "metadata":{"display_phone_number":"970599000000","phone_number_id":"PNID1"},
          "contacts":[{"profile":{"name":"Lina Haddad"},"wa_id":"970599111222"}],
          "messages":[
            {"from":"970599111222","id":"wamid.A","timestamp":"1793865600","type":"text","text":{"body":"قديش سعر لاتيه الفستق؟"}},
            {"from":"970599111222","id":"wamid.B","timestamp":"1793865610","type":"image","image":{"id":"m1","caption":"هاد الي بدي ياه"}},
            {"from":"970599111222","id":"wamid.C","timestamp":"1793865620","type":"interactive","interactive":{"type":"button_reply","button_reply":{"id":"b1","title":"Yes"}}}
          ],
          "statuses":[{"id":"wamid.OUT","status":"read","timestamp":"1793865630","recipient_id":"970599111222"},
                      {"id":"wamid.OUT2","status":"failed","timestamp":"1793865640","errors":[{"code":131047,"title":"Re-engagement message"}]}]
        }}]}]}
        """;

    [Fact]
    public void Accepts_only_the_signature_made_with_our_app_secret()
    {
        var signature = WhatsAppWebhook.ComputeSignature(Payload, Secret);

        Assert.True(WhatsAppWebhook.IsValidSignature(signature, Payload, Secret));
        Assert.False(WhatsAppWebhook.IsValidSignature(signature, Payload + " ", Secret));
        Assert.False(WhatsAppWebhook.IsValidSignature(signature, Payload, "other-secret"));
        Assert.False(WhatsAppWebhook.IsValidSignature("sha256=zz", Payload, Secret));
        Assert.False(WhatsAppWebhook.IsValidSignature(null, Payload, Secret));
    }

    [Fact]
    public void Reads_messages_with_the_customer_name_and_number()
    {
        var parsed = WhatsAppWebhook.Parse(JsonNode.Parse(Payload)!.AsObject());

        Assert.Equal(3, parsed.Messages.Count);
        var first = parsed.Messages[0];
        Assert.Equal("PNID1", first.PhoneNumberId);
        Assert.Equal("970599111222", first.From);
        Assert.Equal("Lina Haddad", first.ProfileName);
        Assert.Equal("قديش سعر لاتيه الفستق؟", first.Text);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1793865600), first.SentAt);
    }

    [Fact]
    public void Media_and_buttons_become_readable_text()
    {
        var parsed = WhatsAppWebhook.Parse(JsonNode.Parse(Payload)!.AsObject());

        Assert.Equal("[image] هاد الي بدي ياه", parsed.Messages[1].Text);
        Assert.Equal("Yes", parsed.Messages[2].Text);
    }

    [Fact]
    public void Reads_delivery_statuses_and_their_errors()
    {
        var parsed = WhatsAppWebhook.Parse(JsonNode.Parse(Payload)!.AsObject());

        Assert.Equal(2, parsed.Statuses.Count);
        Assert.Equal("read", parsed.Statuses[0].Status);
        Assert.Null(parsed.Statuses[0].Error);
        Assert.Equal("131047: Re-engagement message", parsed.Statuses[1].Error);
    }
}
