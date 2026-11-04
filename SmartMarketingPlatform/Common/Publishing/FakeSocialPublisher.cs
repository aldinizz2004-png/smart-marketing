using System.Collections.Concurrent;

namespace SmartMarketingPlatform.Common.Publishing;

// Pretends to publish and returns a realistic post id and URL.
// Test markers in the text: "[simulate-publish-failure]" always fails (transient),
// "[simulate-publish-flaky]" fails on the first attempt only.
public sealed class FakeSocialPublisher : ISocialPublisher
{
    public const string FailureMarker = "[simulate-publish-failure]";
    public const string FlakyMarker = "[simulate-publish-flaky]";

    private readonly ConcurrentDictionary<string, int> _attempts = new();

    public Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        var attempt = _attempts.AddOrUpdate(request.IdempotencyKey, 1, (_, n) => n + 1);

        if (request.Body.Contains(FailureMarker, StringComparison.OrdinalIgnoreCase))
            throw new PublishException("Simulated network timeout.", isTransient: true);
        if (request.Body.Contains(FlakyMarker, StringComparison.OrdinalIgnoreCase) && attempt == 1)
            throw new PublishException("Simulated rate limit (first attempt only).", isTransient: true);

        var postId = Guid.NewGuid().ToString("N")[..12];
        var url = request.PlatformCode switch
        {
            "instagram" => $"https://www.instagram.com/p/{postId}/",
            "facebook" => $"https://www.facebook.com/{request.ExternalAccountId}/posts/{postId}",
            "tiktok" => $"https://www.tiktok.com/@{request.ExternalAccountId}/video/{postId}",
            _ => $"https://{request.PlatformCode}.example/{postId}"
        };
        return Task.FromResult(new PublishResult($"{request.PlatformCode}_{postId}", url));
    }
}
