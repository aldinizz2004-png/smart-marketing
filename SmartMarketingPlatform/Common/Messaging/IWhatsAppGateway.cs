namespace SmartMarketingPlatform.Common.Messaging;

// Sends WhatsApp messages from a business number. Returns WhatsApp's message id ("wamid...").
public interface IWhatsAppGateway
{
    Task<string> SendTextAsync(WhatsAppSender from, string to, string text, CancellationToken cancellationToken);

    // Outside the 24-hour window only pre-approved templates may be sent.
    Task<string> SendTemplateAsync(WhatsAppSender from, string to, string templateName, string languageCode, CancellationToken cancellationToken);
}

public sealed record WhatsAppSender(string PhoneNumberId, string AccessToken);

public sealed class WhatsAppGatewayException(string code, string message, bool outsideWindow = false) : Exception(message)
{
    public string Code { get; } = code;
    public bool OutsideWindow { get; } = outsideWindow;
}

// Development: nothing leaves the machine; the message gets a fake id and is logged.
public sealed class FakeWhatsAppGateway(ILogger<FakeWhatsAppGateway> logger) : IWhatsAppGateway
{
    public Task<string> SendTextAsync(WhatsAppSender from, string to, string text, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fake WhatsApp from {From} to {To}: {Text}", from.PhoneNumberId, to, text);
        return Task.FromResult($"wamid.fake.{Guid.NewGuid():N}");
    }

    public Task<string> SendTemplateAsync(WhatsAppSender from, string to, string templateName, string languageCode, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fake WhatsApp template {Template} ({Language}) from {From} to {To}", templateName, languageCode, from.PhoneNumberId, to);
        return Task.FromResult($"wamid.fake.{Guid.NewGuid():N}");
    }
}
