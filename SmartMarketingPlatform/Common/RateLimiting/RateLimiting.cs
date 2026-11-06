using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartMarketingPlatform.Common.Auth;

namespace SmartMarketingPlatform.Common.RateLimiting;

// "RateLimiting" section. Limits are per minute.
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; init; } = true;
    public int GlobalPerMinute { get; init; } = 300;        // every request, per user (or per IP before login)
    public int AuthPerMinute { get; init; } = 10;           // register + login, per IP: slows password guessing
    public int ExpensivePerMinute { get; init; } = 20;      // AI and external calls, per tenant: protects the bill
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Expensive = "expensive";
}

public static class RateLimitingSetup
{
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = OnRejectedAsync;

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                Fixed(options.Enabled, UserOrIp(http), options.GlobalPerMinute));

            // Sliding window: a burst at the end of one minute and the start of the next is still counted together.
            limiter.AddPolicy(RateLimitPolicies.Auth, http => options.Enabled
                ? RateLimitPartition.GetSlidingWindowLimiter("ip:" + Ip(http), _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPerMinute,
                    Window = Minute,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0
                })
                : RateLimitPartition.GetNoLimiter("off"));

            limiter.AddPolicy(RateLimitPolicies.Expensive, http =>
                Fixed(options.Enabled, Tenant(http) is { } tenant ? "tenant:" + tenant : "ip:" + Ip(http), options.ExpensivePerMinute));
        });
        return services;
    }

    private static RateLimitPartition<string> Fixed(bool enabled, string key, int perMinute) => enabled
        ? RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = perMinute,
            Window = Minute,
            QueueLimit = 0
        })
        : RateLimitPartition.GetNoLimiter("off");

    // The client is told how long to wait, in the same ProblemDetails format as every other error.
    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        var wait = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : Minute;
        var seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiting")
            .LogWarning("Rate limit hit on {Method} {Path} by {Client}", http.Request.Method, http.Request.Path, UserOrIp(http));

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = $"Too many requests. Try again in {seconds} seconds."
            }
        });
    }

    private static string UserOrIp(HttpContext http) =>
        http.User.FindFirstValue(AppClaims.UserId) is { Length: > 0 } user ? "user:" + user : "ip:" + Ip(http);

    private static string? Tenant(HttpContext http) =>
        http.User.FindFirstValue(AppClaims.TenantId) is { Length: > 0 } tenant ? tenant : null;

    private static string Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
