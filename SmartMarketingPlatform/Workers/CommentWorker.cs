using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Workers;

// Classifies new comments without waiting for the owner to press "analyze", so complaints reach them quickly.
// Nothing is replied automatically: the owner still decides.
public sealed class CommentWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CommentOptions> options,
    TimeProvider clock,
    ILogger<CommentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Max(1, options.Value.CheckEverySeconds);
        logger.LogInformation("Comment worker started (every {Seconds} s).", seconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds), clock);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var run = await scope.ServiceProvider.GetRequiredService<ICommentService>().AnalyzePendingAsync(stoppingToken);
                if (run.Comments > 0)
                    logger.LogInformation("Analyzed {Comments} comment(s) for {Businesses} business(es); {Complaints} complaint(s).",
                        run.Comments, run.Businesses, run.Complaints);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Comment worker run failed; it will run again later.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
