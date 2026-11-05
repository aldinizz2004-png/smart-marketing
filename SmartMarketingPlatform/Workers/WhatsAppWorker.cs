using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Workers;

// Answers WhatsApp conversations a few seconds after the customer stops typing.
public sealed class WhatsAppWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WhatsAppOptions> options,
    TimeProvider clock,
    ILogger<WhatsAppWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation("WhatsApp worker started (every {Seconds}s, waits {Debounce}s after the last message).",
            settings.PollSeconds, settings.DebounceSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollSeconds), clock);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handled = await scope.ServiceProvider.GetRequiredService<IWhatsAppInboxService>().ProcessPendingAsync(stoppingToken);
                if (handled > 0) logger.LogInformation("Handled {Count} WhatsApp conversation(s).", handled);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "WhatsApp worker run failed; it will run again later.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
