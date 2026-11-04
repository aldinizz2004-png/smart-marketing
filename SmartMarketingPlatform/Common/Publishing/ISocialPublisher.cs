namespace SmartMarketingPlatform.Common.Publishing;

// The only door to the social networks. Today: FakeSocialPublisher.
// Later: Meta (Facebook/Instagram) and others behind the same interface.
public interface ISocialPublisher
{
    Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken cancellationToken);
}

public sealed record PublishRequest(
    string PlatformCode,
    string ExternalAccountId,
    string AccessToken,
    string Body,
    IReadOnlyList<PublishMedia> Media,
    string IdempotencyKey);

public sealed record PublishMedia(string ObjectKey, string MimeType);

public sealed record PublishResult(string ExternalPostId, string ExternalUrl);

// IsTransient = worth retrying (timeout, rate limit, 5xx). Permanent = invalid token, rejected content ...
public sealed class PublishException(string message, bool isTransient) : Exception(message)
{
    public bool IsTransient { get; } = isTransient;
}
