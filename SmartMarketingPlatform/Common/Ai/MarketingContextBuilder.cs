using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// Collects what the AI needs to know, from the database, in one place.
public sealed class MarketingContextBuilder(MarketingDbContext db, TimeProvider clock) : IMarketingContextBuilder
{
    private const int MaxItems = 20;
    private static readonly string[] DefaultChannels = ["instagram", "facebook"];

    public async Task<MarketingContext> BuildAsync(Guid businessId, Guid? campaignId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var business = await db.Businesses.AsNoTracking().FirstAsync(b => b.Id == businessId, cancellationToken);
        var brand = await db.BrandProfiles.AsNoTracking().Include(p => p.Terms)
            .FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);
        var policy = await db.AutomationPolicies.AsNoTracking().Include(p => p.AllowedPlatforms)
            .FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);

        var campaign = campaignId is null ? null : await db.Campaigns.AsNoTracking()
            .Include(c => c.CampaignProducts).Include(c => c.CampaignOffers).Include(c => c.CampaignAudiences)
            .AsSplitQuery()
            .FirstAsync(c => c.Id == campaignId, cancellationToken);

        // A campaign uses what it selected; with nothing selected, the business's current catalogue.
        var productIds = campaign?.CampaignProducts.Select(x => x.ProductId).ToList() ?? [];
        var offerIds = campaign?.CampaignOffers.Select(x => x.OfferId).ToList() ?? [];
        var audienceIds = campaign?.CampaignAudiences.Select(x => x.AudienceSegmentId).ToList() ?? [];

        var products = await db.Products.AsNoTracking()
            .Where(p => p.BusinessId == businessId && (productIds.Count > 0 ? productIds.Contains(p.Id) : p.IsActive))
            .OrderBy(p => p.Name).Take(MaxItems)
            .Select(p => new ProductInfo(p.Id, p.Name, p.Price, p.Currency))
            .ToListAsync(cancellationToken);

        var offers = await db.Offers.AsNoTracking()
            .Where(o => o.BusinessId == businessId && (offerIds.Count > 0
                ? offerIds.Contains(o.Id)
                : o.StatusId == StatusIds.Offer.Active && o.EndsAt > now))
            .OrderBy(o => o.EndsAt).Take(MaxItems)
            .Select(o => new OfferInfo(o.Id, o.Title, o.DiscountPercent, o.OfferPrice, o.EndsAt))
            .ToListAsync(cancellationToken);

        var audiences = await db.AudienceSegments.AsNoTracking()
            .Where(a => a.BusinessId == businessId && (audienceIds.Count == 0 || audienceIds.Contains(a.Id)))
            .OrderBy(a => a.Name).Take(MaxItems)
            .Select(a => new AudienceInfo(a.Id, a.Name, a.Description))
            .ToListAsync(cancellationToken);

        // Channels: the owner's allowed platforms, else the connected accounts, else a sensible default.
        var channels = policy?.AllowedPlatforms.Select(p => p.PlatformCode).OrderBy(c => c).ToList() ?? [];
        if (channels.Count == 0)
            channels = await db.SocialAccounts.AsNoTracking()
                .Where(s => s.BusinessId == businessId && s.StatusId == StatusIds.SocialAccount.Connected)
                .Select(s => s.PlatformCode).Distinct().OrderBy(c => c)
                .ToListAsync(cancellationToken);
        if (channels.Count == 0)
            channels = [.. DefaultChannels];

        return new MarketingContext(
            business.Id, business.Name, business.Category, business.City, business.TimeZoneId, business.DefaultCurrency,
            brand?.Dialect, brand?.Tone, brand?.EmojiLevel ?? 1,
            brand?.Terms.Where(t => t.Rule == BrandTermRule.Preferred).Select(t => t.Term).ToList() ?? [],
            brand?.Terms.Where(t => t.Rule == BrandTermRule.Prohibited).Select(t => t.Term).ToList() ?? [],
            products, offers, audiences, channels,
            policy?.MaxPostsPerDay ?? 3,
            policy?.PublishWindowStart?.ToString("HH:mm", CultureInfo.InvariantCulture),
            policy?.PublishWindowEnd?.ToString("HH:mm", CultureInfo.InvariantCulture),
            campaign is null ? null : new CampaignInfo(campaign.Id, campaign.Name, campaign.Objective, campaign.GoalStatement,
                campaign.Budget, campaign.BudgetCurrency, campaign.StartsAt, campaign.EndsAt));
    }
}
