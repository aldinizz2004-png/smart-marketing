using System.Security.Claims;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Auth;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("There is no current HTTP request.");

    public Guid UserId => ReadGuid(AppClaims.UserId);
    public Guid TenantId => ReadGuid(AppClaims.TenantId);

    public MembershipRole Role =>
        Enum.TryParse<MembershipRole>(Principal.FindFirstValue(AppClaims.Role), out var role)
            ? role
            : throw new UnauthorizedException("The access token has no valid role.");

    private Guid ReadGuid(string claimType) =>
        Guid.TryParse(Principal.FindFirstValue(claimType), out var id)
            ? id
            : throw new UnauthorizedException($"The access token has no valid '{claimType}' claim.");
}
