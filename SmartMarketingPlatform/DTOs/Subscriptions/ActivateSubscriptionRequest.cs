using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Subscriptions;

// Confirms the payment of a pending subscription. Today it is called by the owner;
// later it is called by the payment provider's webhook.
public sealed class ActivateSubscriptionRequest
{
    [Required, MaxLength(200)]
    public string? PaymentReference { get; init; }
}
