using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Competitors;

// Reads the public posts of an account the owner wants to learn from.
// Competitors:Provider = Fake (demo data) or Instagram (Graph API Business Discovery).
public interface ICompetitorSource
{
    bool Supports(string platformCode);

    Task<CompetitorSnapshot> FetchAsync(CompetitorTarget target, int maxPosts, CancellationToken cancellationToken);
}

public sealed record CompetitorTarget(string PlatformCode, string Handle, string NormalizedUrl);

public sealed record CompetitorSnapshot(string? DisplayName, long? Followers, IReadOnlyList<CompetitorPost> Posts);

public sealed record CompetitorPost(
    string ExternalPostId,
    string? PostUrl,
    DateTimeOffset? PostedAt,
    ContentFormat Format,
    string? Caption,
    int? Likes,
    int? Comments,
    long? Views);

// SourceProblem = the account itself cannot be read (unknown, private, personal account):
// retrying will not help, so the tracked source is marked Failed.
public sealed class CompetitorSourceException(string code, string message, bool sourceProblem = false) : Exception(message)
{
    public string Code { get; } = code;
    public bool SourceProblem { get; } = sourceProblem;
}
