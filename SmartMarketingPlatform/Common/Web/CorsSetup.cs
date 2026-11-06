using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Logging;

namespace SmartMarketingPlatform.Common.Web;

// The web app runs on its own origin (Vite on :5173 in development), so the browser needs CORS to call the API.
// Only the origins listed in Cors:AllowedOrigins are allowed; credentials are on for the refresh cookie.
public static class CorsSetup
{
    public const string SectionName = "Cors";

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection($"{SectionName}:AllowedOrigins").Get<string[]>() ?? [];

        return services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .AllowAnyMethod()
            .WithHeaders("Authorization", "Content-Type", "Accept-Language", CorrelationId.HeaderName, RefreshCookie.TransportHeader)
            .WithExposedHeaders(CorrelationId.HeaderName, "Location", "Retry-After")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
    }
}
