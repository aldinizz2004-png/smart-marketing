using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// One row per subscription period. A renewal creates a NEW row that points to the
// previous one, so history (renewals, cancellations, expirations) is never overwritten.
public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }                    // the tenant is the paying customer
    public SubscriptionDuration Duration { get; set; }
    public decimal Price { get; set; }                    // price paid for this period
    public string Currency { get; set; } = "USD";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }            // StartsAt + Duration
    public int StatusId { get; set; }                     // Statuses (Category = "Subscription")
    public Guid? RenewedFromSubscriptionId { get; set; }  // the period this one renews
    public DateTimeOffset? CanceledAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Status Status { get; set; } = null!;
    public Subscription? RenewedFrom { get; set; }
    public Subscription? RenewedBy { get; set; }

    public static DateTimeOffset CalculateEndsAt(DateTimeOffset startsAt, SubscriptionDuration duration)
        => startsAt.AddMonths((int)duration);

    // The subscription that grants access right now: Active and inside its period.
    public bool GrantsAccessAt(DateTimeOffset now)
        => StatusId == StatusIds.Subscription.Active && StartsAt <= now && now < EndsAt;
}