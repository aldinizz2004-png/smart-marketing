namespace SmartMarketingPlatform.Common.Publishing;

public interface ISocialPublisher
{
    // False when the provider holds the platform tokens itself (Postproxy), so accounts have no CredentialRef.
    bool RequiresAccessToken { get; }

    Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken cancellationToken);
}

public sealed record PublishRequest(
    string PlatformCode,
    string ExternalAccountId,
    string? AccessToken,
    string Body,
    IReadOnlyList<PublishMedia> Media,
    string IdempotencyKey);

public sealed record PublishMedia(string ObjectKey, string MimeType);

public sealed record PublishResult(string ExternalPostId, string? ExternalUrl);

// IsTransient = worth retrying (timeout, rate limit, 5xx). Permanent = invalid token, rejected content ...
public sealed class PublishException(string message, bool isTransient) : Exception(message)
{
    public bool IsTransient { get; } = isTransient;
}
