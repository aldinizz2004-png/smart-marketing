using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// Many-to-many between Tenant and AppUser, with a role on the link.
public class TenantMembership
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public MembershipRole Role { get; set; }
    public int StatusId { get; set; }                     // Statuses (Category = "Membership")
    public DateTimeOffset JoinedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public Status Status { get; set; } = null!;
}