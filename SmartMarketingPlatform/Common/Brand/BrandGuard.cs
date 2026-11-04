using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Brand;

public sealed class BrandGuard(MarketingDbContext db) : IBrandGuard
{
    public async Task<IReadOnlyList<string>> FindProhibitedTermsAsync(Guid businessId, string text, CancellationToken cancellationToken)
    {
        var terms = await db.BrandTerms.AsNoTracking()
            .Where(t => t.BusinessId == businessId && t.Rule == BrandTermRule.Prohibited)
            .Select(t => t.Term)
            .ToListAsync(cancellationToken);

        // Whole words only ("cheap" is found in "Cheap!", not in "cheapest"... and works for Arabic letters too).
        return terms
            .Where(term => Regex.IsMatch(text, $@"(?<!\w){Regex.Escape(term)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToList();
    }
}
