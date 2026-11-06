using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SmartMarketingPlatform.Common.Auth;

namespace SmartMarketingPlatform.Common.Logging;

// Structured logs: every line is an event with named properties (CorrelationId, TenantId, StatusCode ...),
// not just text, so they can be filtered ("all errors of tenant X today") in any log tool.
//   Console: readable in Development, JSON elsewhere (Docker collects stdout).
//   File:    JSON, one file per day in App_Data/logs (LogFiles section); levels in the Serilog section.
public static class LoggingSetup
{
    private const string ReadableTemplate =
        "{Level:u3} {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddAppLogging(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();
        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "SmartMarketingPlatform");

            if (development)
                logger.WriteTo.Console(outputTemplate: ReadableTemplate);
            else
                logger.WriteTo.Console(new CompactJsonFormatter());

            // Relative to the app folder, not to wherever "dotnet run" was started.
            var files = builder.Configuration.GetSection("LogFiles");
            if (files.GetValue("Enabled", true))
                logger.WriteTo.File(
                    new CompactJsonFormatter(),
                    Path.Combine(builder.Environment.ContentRootPath, files["Path"] ?? "App_Data/logs/api-.json"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: files.GetValue("RetainedDays", 14));
        });
        return builder;
    }

    // One summary line per request instead of the framework's several lines.
    public static IApplicationBuilder UseAppRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
            options.GetLevel = (http, elapsedMs, exception) =>
                exception is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
                : http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : elapsedMs > 5000 ? LogEventLevel.Warning
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, http) =>
            {
                diagnostics.Set("UserId", http.User.FindFirst(AppClaims.UserId)?.Value);
                diagnostics.Set("TenantId", http.User.FindFirst(AppClaims.TenantId)?.Value);
                diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
            };
        });
}
