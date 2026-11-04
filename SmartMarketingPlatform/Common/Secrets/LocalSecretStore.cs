using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Options;

namespace SmartMarketingPlatform.Common.Secrets;

// Each secret is encrypted with ASP.NET Core Data Protection and written to its own file.
// Reference format: "local:{guid}".
public sealed class LocalSecretStore : ISecretStore
{
    private const string Prefix = "local:";
    private readonly string _root;
    private readonly IDataProtector _protector;

    public LocalSecretStore(IOptions<StorageOptions> options, IWebHostEnvironment environment, IDataProtectionProvider dataProtection)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.SecretsRoot));
        Directory.CreateDirectory(_root);
        _protector = dataProtection.CreateProtector("SmartMarketingPlatform.SocialCredentials.v1");
    }

    public async Task<string> SaveAsync(string secret, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(PathOf(id), _protector.Protect(secret), cancellationToken);
        return Prefix + id;
    }

    public async Task<string?> GetAsync(string reference, CancellationToken cancellationToken)
    {
        var path = PathOf(IdOf(reference));
        return File.Exists(path) ? _protector.Unprotect(await File.ReadAllTextAsync(path, cancellationToken)) : null;
    }

    public Task DeleteAsync(string reference, CancellationToken cancellationToken)
    {
        var path = PathOf(IdOf(reference));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static string IdOf(string reference) =>
        reference.StartsWith(Prefix, StringComparison.Ordinal) && Guid.TryParseExact(reference[Prefix.Length..], "N", out var id)
            ? id.ToString("N")
            : throw new InvalidOperationException("Unknown secret reference.");

    private string PathOf(string id) => Path.Combine(_root, id + ".secret");
}
