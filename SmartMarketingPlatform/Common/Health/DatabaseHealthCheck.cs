using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartMarketingPlatform.Data;

namespace SmartMarketingPlatform.Common.Health;

// Healthy   = database reachable and all migrations applied.
// Degraded  = reachable, but migrations are pending (app runs, schema may be old).
// Unhealthy = cannot reach the database -> /health returns 503.
public sealed class DatabaseHealthCheck(MarketingDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Cannot connect to the database.");

            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            return pending.Count == 0
                ? HealthCheckResult.Healthy("Database is reachable and up to date.")
                : HealthCheckResult.Degraded($"{pending.Count} pending migration(s).",
                    data: new Dictionary<string, object> { ["pendingMigrations"] = pending });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", ex);
        }
    }
}
