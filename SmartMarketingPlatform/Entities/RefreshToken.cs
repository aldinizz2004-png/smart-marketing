namespace SmartMarketingPlatform.Entities;

// A long-lived token that buys new short-lived access tokens. Only its SHA-256 hash is stored.
// Every use replaces it with a new one of the same family (rotation); using an old one again means
// it was stolen, so the whole family (that login session) is revoked.
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid FamilyId { get; set; }                          // one login = one family
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }                   // rotated, logout, logout_all, reuse_detected
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }

    public AppUser User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
