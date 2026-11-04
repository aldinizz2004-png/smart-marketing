namespace SmartMarketingPlatform.Entities;

// The paying customer account. One tenant can own several businesses.
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StatusId { get; set; }                     // Statuses (Category = "Tenant")
    public DateTimeOffset CreatedAt { get; set; }

    public Status Status { get; set; } = null!;
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}