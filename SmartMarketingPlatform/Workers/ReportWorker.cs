using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Reports;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Workers;

// Checks regularly whether a business's weekly report moment (e.g. Sunday 09:00 local time) has passed
// and its report is missing. Checking often but generating once makes it safe across restarts.
public sealed class ReportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportOptions> options,
    TimeProvider clock,
    ILogger<ReportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation("Report worker started (every {Minutes} min, weekly on {Day} at {Hour}:00 business time).",
            settings.CheckEveryMinutes, settings.WeeklyDay, settings.WeeklyHour);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.CheckEveryMinutes), clock);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var made = await scope.ServiceProvider.GetRequiredService<IReportService>().GenerateDueReportsAsync(stoppingToken);
                if (made > 0) logger.LogInformation("Sent {Count} weekly report(s).", made);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Report worker run failed; it will run again later.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
