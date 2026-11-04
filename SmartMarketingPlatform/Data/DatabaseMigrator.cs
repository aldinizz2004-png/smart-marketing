using Microsoft.EntityFrameworkCore;

namespace SmartMarketingPlatform.Data;

// Applies the existing EF Core migrations when the app starts (enabled in Docker with
// Database__ApplyMigrationsOnStartup=true). It retries because the database container
// can need a few more seconds after it reports "healthy" to accept logins.
public static class DatabaseMigrator
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);

    public static async Task ApplyMigrationsAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            return;

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator).FullName!);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count == 0)
                {
                    logger.LogInformation("Database is up to date; no migrations to apply.");
                    return;
                }

                logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
                // Creates the database if it does not exist, then runs every pending migration in order.
                // EF Core takes a database lock, so two app instances cannot migrate at the same time.
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Error}. Retrying in {Delay}s.",
                    attempt, MaxAttempts, ex.Message, Delay.TotalSeconds);
                await Task.Delay(Delay, cancellationToken);
            }
        }
    }
}
