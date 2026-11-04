namespace SmartMarketingPlatform.Entities;

// A login identity. Local accounts use email + password (AuthSubject = "local|{id}");
// an external provider (e.g. Google) can be added later with its own AuthSubject and no password.
public class AppUser
{
    public Guid Id { get; set; }
    public string AuthSubject { get; set; } = string.Empty;   // unique id of the identity, e.g. "local|{id}"
    public string Email { get; set; } = string.Empty;         // unique, stored lower-case
    public string? PasswordHash { get; set; }                 // PBKDF2 hash; null for external-provider users
    public string? FullName { get; set; }
    public string PreferredLanguage { get; set; } = "ar";     // "ar" or "en", picks the translation file
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
