using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Tenants;

// PATCH: change the role, enable/disable the membership, or both.
public sealed class UpdateMemberRequest
{
    [EnumDataType(typeof(MembershipRole))]
    public MembershipRole? Role { get; init; }

    public bool? IsActive { get; init; }
}
