using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Data;

namespace SmartMarketingPlatform.Common.Competitors;

public sealed record CopyMatch(Guid SourcePostId, string Source, double Similarity);

// Learning from a competitor is fine; publishing their caption is not. Every post we save
// (written by the AI or by a person) is compared with the captions collected from tracked sources.
public interface ICopyGuard
{
    Task<CopyMatch?> FindCopyAsync(Guid businessId, string text, CancellationToken cancellationToken);
}

public sealed class CopyGuard(MarketingDbContext db, IOptions<CompetitorOptions> options, TimeProvider clock) : ICopyGuard
{
    private const int MaxCaptions = 500;

    public async Task<CopyMatch?> FindCopyAsync(Guid businessId, string text, CancellationToken cancellationToken)
    {
        var since = clock.GetUtcNow().AddDays(-options.Value.AnalysisWindowDays * 2);
        var captions = await db.SourcePosts.AsNoTracking()
            .Where(p => p.TrackedSource.BusinessId == businessId && p.CaptionText != null && p.CapturedAt >= since)
            .OrderByDescending(p => p.CapturedAt)
            .Take(MaxCaptions)
            .Select(p => new { p.Id, p.CaptionText, Source = p.TrackedSource.DisplayName ?? p.TrackedSource.Handle ?? p.TrackedSource.Host })
            .ToListAsync(cancellationToken);

        var best = captions
            .Select(c => new CopyMatch(c.Id, c.Source, PostText.Containment(text, c.CaptionText!)))
            .MaxBy(m => m.Similarity);

        return best is not null && best.Similarity >= options.Value.MaxCopySimilarity ? best : null;
    }
}
