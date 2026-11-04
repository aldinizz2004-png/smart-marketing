using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Auth;

// Who is calling, read from the validated JWT. Services use TenantId from here,
// never from the URL or the body: this is what keeps tenants isolated from each other.
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid TenantId { get; }
    MembershipRole Role { get; }
}
