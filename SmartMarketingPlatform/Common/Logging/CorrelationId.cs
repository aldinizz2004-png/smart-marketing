using System.Text.RegularExpressions;
using Serilog;
using Serilog.Context;
using SmartMarketingPlatform.Common.Auth;

namespace SmartMarketingPlatform.Common.Logging;

// One id per request, in the X-Correlation-Id response header, in every log line of that request
// and in error responses. A caller (a frontend, another service) may send its own id to follow a request across systems.
public static partial class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    // Only short, plain ids are accepted from outside: anything else could break or forge log lines.
    public static string Resolve(string? incoming, Func<string> createNew) =>
        !string.IsNullOrWhiteSpace(incoming) && SafeId().IsMatch(incoming) ? incoming : createNew();

    public static string? Of(HttpContext http) => http.Items[ItemKey] as string;

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafeId();
}

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http)
    {
        var id = CorrelationId.Resolve(http.Request.Headers[CorrelationId.HeaderName].FirstOrDefault(), () => Guid.NewGuid().ToString("N"));
        http.Items[CorrelationId.ItemKey] = id;
        http.Response.OnStarting(() =>
        {
            http.Response.Headers[CorrelationId.HeaderName] = id;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationId.ItemKey, id))
            await next(http);
    }
}

// After authentication: who is calling and which business is touched go on every log line of the request.
// Route values are copied to the request summary now, because the error handler clears them before it ends.
public sealed class RequestLogContextMiddleware(RequestDelegate next, IDiagnosticContext diagnostics)
{
    public async Task InvokeAsync(HttpContext http)
    {
        var userId = http.User.FindFirst(AppClaims.UserId)?.Value;
        var tenantId = http.User.FindFirst(AppClaims.TenantId)?.Value;
        var businessId = http.GetRouteValue("businessId")?.ToString();
        if (businessId is not null)
            diagnostics.Set("BusinessId", businessId);
        if (http.GetEndpoint()?.DisplayName is { } endpoint)
            diagnostics.Set("Endpoint", endpoint);

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("BusinessId", businessId))
            await next(http);
    }
}
