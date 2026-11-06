using System.Security.Cryptography;
using System.Text;

namespace SmartMarketingPlatform.Common.Auth;

// 64 random bytes, URL-safe. The token is only ever shown to the client; the database keeps its hash.
// A fast hash is enough here: the value is random and long, so it cannot be guessed like a password.
public static class RefreshTokenValue
{
    public static string New() => Base64Url(RandomNumberGenerator.GetBytes(64));

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
