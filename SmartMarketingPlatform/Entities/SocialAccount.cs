namespace SmartMarketingPlatform.Entities;

// The business's own connected account on a platform (its page / handle).
public class SocialAccount
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string PlatformCode { get; set; } = string.Empty;    // FK -> Platform
    public string ExternalAccountId { get; set; } = string.Empty;
    public string? Handle { get; set; }
    public string? CredentialRef { get; set; }                  // reference to a secret store, never the token itself
    public int StatusId { get; set; }                           // Statuses (Category = "SocialAccount")
    public DateTimeOffset CreatedAt { get; set; }

    public Business Business { get; set; } = null!;
    public Platform Platform { get; set; } = null!;
    public Status Status { get; set; } = null!;
}
