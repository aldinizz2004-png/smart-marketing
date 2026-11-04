using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Auth;

public interface ITokenService
{
    // One token = one user working on one tenant with one role.
    (string Token, DateTimeOffset ExpiresAt) CreateToken(AppUser user, Guid tenantId, MembershipRole role);
}
