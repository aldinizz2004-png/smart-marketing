namespace SmartMarketingPlatform.Common.Auth;

// Browsers ask for the refresh token in an httpOnly cookie with "X-Auth-Transport: cookie", so page scripts
// never see it. Other clients (mobile, scripts) keep sending it in the body.
// The custom header is also the CSRF guard: a cross-site form cannot set it, so the cookie alone is never enough.
public static class RefreshCookie
{
    public const string Name = "smp_refresh";
    public const string TransportHeader = "X-Auth-Transport";
    public const string CookieTransport = "cookie";
    public const string Path = "/api/auth";

    public static bool IsRequested(HttpRequest request) =>
        string.Equals(request.Headers[TransportHeader], CookieTransport, StringComparison.OrdinalIgnoreCase);

    public static string? Read(HttpRequest request) =>
        IsRequested(request) ? request.Cookies[Name] : null;

    public static void Write(HttpContext http, string token, DateTimeOffset expiresAt) =>
        http.Response.Cookies.Append(Name, token, Options(http, expiresAt));

    public static void Clear(HttpContext http) =>
        http.Response.Cookies.Delete(Name, Options(http, null));

    // Secure everywhere except plain-HTTP development; production sits behind TLS.
    private static CookieOptions Options(HttpContext http, DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps || !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true
    };
}
