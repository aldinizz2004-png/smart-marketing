using SmartMarketingPlatform.Common.Auth;

namespace SmartMarketingPlatform.UnitTests.Auth;

public class RefreshTokenValueTests
{
    [Fact]
    public void New_tokens_are_long_url_safe_and_unique()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => RefreshTokenValue.New()).ToList();

        Assert.All(tokens, t =>
        {
            Assert.Equal(86, t.Length);                                   // 64 bytes in base64url, no padding
            Assert.Matches("^[A-Za-z0-9_-]+$", t);
        });
        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public void The_hash_is_stable_and_never_the_token_itself()
    {
        var token = RefreshTokenValue.New();

        var hash = RefreshTokenValue.Hash(token);

        Assert.Equal(hash, RefreshTokenValue.Hash(token));
        Assert.Equal(64, hash.Length);
        Assert.NotEqual(token, hash);
        Assert.NotEqual(hash, RefreshTokenValue.Hash(token + "x"));
    }
}
