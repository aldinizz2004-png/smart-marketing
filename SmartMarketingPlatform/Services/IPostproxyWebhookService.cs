using System.Text.Json.Nodes;

namespace SmartMarketingPlatform.Services;

public interface IPostproxyWebhookService
{
    Task<string> HandleAsync(JsonObject webhookEvent, CancellationToken cancellationToken);
}
