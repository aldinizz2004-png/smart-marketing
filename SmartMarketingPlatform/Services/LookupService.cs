using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Lookups;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class LookupService(MarketingDbContext db, TranslationService translations) : ILookupService
{
    public async Task<IReadOnlyList<StatusResponse>> GetStatusesAsync(string category, string language, CancellationToken cancellationToken)
    {
        var statuses = await db.Statuses.AsNoTracking()
            .Where(s => s.Category == category && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        // An unknown category is a missing resource, not an empty list.
        if (statuses.Count == 0)
            throw new NotFoundException($"Status category '{category}' was not found.");

        return statuses
            .Select(s => new StatusResponse(s.Id, s.Code, s.TranslationKey, translations.Translate(s.TranslationKey, language)))
            .ToList();
    }

    public async Task<IReadOnlyList<PlatformResponse>> GetPlatformsAsync(string language, CancellationToken cancellationToken)
    {
        var platforms = await db.Platforms.AsNoTracking()
            .Include(p => p.Domains)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return platforms.Select(p => ToResponse(p, language)).ToList();
    }

    public async Task<PlatformResponse> GetPlatformAsync(string code, string language, CancellationToken cancellationToken)
    {
        var platform = await db.Platforms.AsNoTracking()
            .Include(p => p.Domains)
            .FirstOrDefaultAsync(p => p.Code == code && p.IsActive, cancellationToken)
            ?? throw new NotFoundException($"Platform '{code}' was not found.");

        return ToResponse(platform, language);
    }

    // The only place a Platform entity becomes a response.
    private PlatformResponse ToResponse(Platform p, string language) => new(
        p.Code,
        p.DisplayNameKey,
        translations.Translate(p.DisplayNameKey, language),
        p.SupportsPublishing,
        p.SupportsMessaging,
        p.SupportsCompetitorTracking,
        p.Domains.Select(d => d.Domain).OrderBy(d => d).ToList());
}
