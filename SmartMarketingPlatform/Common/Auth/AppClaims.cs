namespace SmartMarketingPlatform.Common.Auth;

// Claim names inside our JWT. Short names, no Microsoft URI mapping (MapInboundClaims = false).
public static class AppClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string TenantId = "tid";     // the tenant this token works on
    public const string Role = "role";        // the user's MembershipRole in that tenant
}
