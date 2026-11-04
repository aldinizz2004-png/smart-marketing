using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Workers;

// Runs inside the API process (a hosted BackgroundService). On each tick it publishes the due jobs;
// every MetricsEverySeconds it collects metrics. Each tick uses its own DI scope (own DbContext),
// and an error in one tick is logged and never stops the loop.
public sealed class PublishingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PublishingOptions> options,
    TimeProvider clock,
    ILogger<PublishingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation("Publishing worker started (every {Poll}s, metrics every {Metrics}s).", settings.PollSeconds, settings.MetricsEverySeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollSeconds), clock);
        var nextMetrics = DateTimeOffset.MinValue;

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPublicationProcessor>();

                await processor.RecoverStuckJobsAsync(stoppingToken);
                var published = await processor.PublishDueJobsAsync(stoppingToken);
                if (published > 0) logger.LogInformation("Published {Count} post(s).", published);

                if (clock.GetUtcNow() >= nextMetrics)
                {
                    await processor.CollectMetricsAsync(null, stoppingToken);
                    nextMetrics = clock.GetUtcNow().AddSeconds(settings.MetricsEverySeconds);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Publishing worker tick failed; it will run again on the next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
