using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Brand;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed partial class BrandService(MarketingDbContext db, IBusinessAccess access) : IBrandService
{
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();

    public async Task<BrandProfileResponse> GetProfileAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var profile = await LoadProfileAsync(businessId, cancellationToken)
                      ?? throw new NotFoundException("This business has no brand profile yet. Create it with PUT.");
        return ToResponse(profile);
    }

    public async Task<(BrandProfileResponse Profile, bool Created)> UpsertProfileAsync(
        Guid businessId, BrandProfileRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var colors = request.ColorPalette ?? [];
        var badColor = colors.FirstOrDefault(c => !HexColor().IsMatch(c ?? string.Empty));
        if (badColor is not null)
            throw new BadRequestException($"'{badColor}' is not a color like #1F3A5F.");

        var profile = await LoadProfileAsync(businessId, cancellationToken);
        var created = profile is null;
        if (profile is null)
        {
            profile = new BrandProfile { BusinessId = businessId };
            db.BrandProfiles.Add(profile);
        }

        profile.Dialect = request.Dialect!.Trim();
        profile.Tone = request.Tone!.Trim();
        profile.EmojiLevel = request.EmojiLevel!.Value;
        profile.Guidelines = request.Guidelines?.Trim();
        profile.ColorPaletteJson = colors.Count == 0 ? null : JsonSerializer.Serialize(colors.Select(c => c.ToUpperInvariant()));

        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(profile), created);
    }

    public async Task<IReadOnlyList<BrandTermResponse>> GetTermsAsync(Guid businessId, BrandTermRule? rule, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return await db.BrandTerms.AsNoTracking()
            .Where(t => t.BusinessId == businessId && (rule == null || t.Rule == rule))
            .OrderBy(t => t.Rule).ThenBy(t => t.Term)
            .Select(t => new BrandTermResponse(t.Id, t.Term, t.Rule, t.Note))
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandTermResponse> AddTermAsync(Guid businessId, BrandTermRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        // BrandTerms hang off the BrandProfile (FK BusinessId -> BrandProfiles).
        if (!await db.BrandProfiles.AnyAsync(p => p.BusinessId == businessId, cancellationToken))
            throw new ConflictException("Create the brand profile first (PUT .../brand-profile).");

        var term = request.Term!.Trim();
        var rule = request.Rule!.Value;
        if (await db.BrandTerms.AnyAsync(t => t.BusinessId == businessId && t.Rule == rule && t.Term == term, cancellationToken))
            throw new ConflictException($"'{term}' is already a {rule} term.");

        var entity = new BrandTerm { BusinessId = businessId, Term = term, Rule = rule, Note = request.Note?.Trim() };
        db.BrandTerms.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new BrandTermResponse(entity.Id, entity.Term, entity.Rule, entity.Note);
    }

    public async Task DeleteTermAsync(Guid businessId, Guid termId, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var term = await db.BrandTerms.FirstOrDefaultAsync(t => t.Id == termId && t.BusinessId == businessId, cancellationToken)
                   ?? throw new NotFoundException("Brand term not found.");

        db.BrandTerms.Remove(term);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<BrandProfile?> LoadProfileAsync(Guid businessId, CancellationToken cancellationToken) =>
        db.BrandProfiles.Include(p => p.Terms).FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);

    private static BrandProfileResponse ToResponse(BrandProfile p) => new(
        p.BusinessId,
        p.Dialect,
        p.Tone,
        p.EmojiLevel,
        p.Guidelines,
        p.ColorPaletteJson is null ? [] : JsonSerializer.Deserialize<List<string>>(p.ColorPaletteJson) ?? [],
        p.Terms.OrderBy(t => t.Rule).ThenBy(t => t.Term).Select(t => new BrandTermResponse(t.Id, t.Term, t.Rule, t.Note)).ToList(),
        p.UpdatedAt);
}
