namespace SmartMarketingPlatform.Common.Messaging;

// WhatsApp Cloud API (Meta). Each business connects its own number as a social account:
// platformCode "whatsapp", externalAccountId = the phone number id, accessToken = its token.
// AppSecret and VerifyToken belong to OUR Meta app and secure the webhook.
public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string Provider { get; set; } = "Fake";             // Fake | Meta
    public string GraphBaseUrl { get; set; } = "https://graph.facebook.com/";
    public string ApiVersion { get; set; } = "v23.0";
    public string AppSecret { get; set; } = string.Empty;
    public string VerifyToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 20;

    public bool WorkerEnabled { get; set; } = true;
    public int PollSeconds { get; set; } = 5;

    // Customers often send several short messages in a row: wait a little and answer them together.
    public int DebounceSeconds { get; set; } = 4;
    public int HistoryMessages { get; set; } = 12;
    public int MaxConversationsPerRun { get; set; } = 10;
    public decimal MinAutoReplyConfidence { get; set; } = 0.7m;
    public int ReplyWindowHours { get; set; } = 24;
    public bool NotifyOwnerByEmail { get; set; } = true;
}
