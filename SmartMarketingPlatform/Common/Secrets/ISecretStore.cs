namespace SmartMarketingPlatform.Common.Secrets;

// Social media access tokens are secrets: the database stores only a reference (CredentialRef).
// Development: encrypted files on disk. Production: a vault (e.g. Azure Key Vault) behind the same interface.
public interface ISecretStore
{
    Task<string> SaveAsync(string secret, CancellationToken cancellationToken);           // returns the reference
    Task<string?> GetAsync(string reference, CancellationToken cancellationToken);
    Task DeleteAsync(string reference, CancellationToken cancellationToken);
}
