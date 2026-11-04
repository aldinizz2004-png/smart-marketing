using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartMarketingPlatform.Common.Health;

// Writes /health as readable JSON instead of the default plain "Healthy" text.
// Exception details are not included, only the description.
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, Json));
    }
}
