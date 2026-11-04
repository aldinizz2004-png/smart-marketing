using System.ComponentModel.DataAnnotations;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Tenants;

// Adds an already registered user to the current tenant.
public sealed class AddMemberRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [Required, EnumDataType(typeof(MembershipRole))]
    public MembershipRole? Role { get; init; }
}
