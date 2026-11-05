using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Workers;

// Keeps the tracked sources fresh (one scan per source per day by default) and refreshes the
// insights of a business once a week when new posts were collected.
public sealed class CompetitorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CompetitorOptions> options,
    TimeProvider clock,
    ILogger<CompetitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation("Competitor worker started (every {Minutes} min, scan every {Hours} h, analysis every {Days} days).",
            settings.CheckEveryMinutes, settings.ScanEveryHours, settings.AnalyzeEveryDays);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.CheckEveryMinutes), clock);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var (scanned, analyzed) = await scope.ServiceProvider.GetRequiredService<ICompetitorService>().RunScheduledAsync(stoppingToken);
                if (scanned + analyzed > 0)
                    logger.LogInformation("Scanned {Scanned} tracked source(s), refreshed insights of {Analyzed} business(es).", scanned, analyzed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Competitor worker run failed; it will run again later.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
