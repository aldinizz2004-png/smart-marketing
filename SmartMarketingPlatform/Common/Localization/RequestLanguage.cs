using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Common.Localization;

// Decides the response language: ?lang=ar wins, otherwise the Accept-Language header,
// otherwise English. The chosen language is echoed back in the Content-Language header.
public static class RequestLanguage
{
    public static string Resolve(HttpContext httpContext, string? lang)
    {
        var language = !string.IsNullOrWhiteSpace(lang)
            ? Normalize(lang)
            : FromHeader(httpContext.Request) ?? TranslationService.DefaultLanguage;

        if (!httpContext.Response.HasStarted)
            httpContext.Response.Headers.ContentLanguage = language;

        return language;
    }

    // For code that has no "lang" parameter of its own (e.g. status texts inside responses).
    public static string Current(HttpContext? httpContext) =>
        httpContext is null
            ? TranslationService.DefaultLanguage
            : Resolve(httpContext, httpContext.Request.Query["lang"].ToString());

    // "ar-PS,ar;q=0.9,en;q=0.8" -> "ar"
    private static string? FromHeader(HttpRequest request)
    {
        var best = request.GetTypedHeaders().AcceptLanguage
            .Where(h => h.Value.HasValue && h.Value.Value != "*")
            .OrderByDescending(h => h.Quality ?? 1)
            .FirstOrDefault();

        return best is null ? null : Normalize(best.Value.Value!);
    }

    private static string Normalize(string tag) => tag.Split('-')[0].Trim().ToLowerInvariant();
}
