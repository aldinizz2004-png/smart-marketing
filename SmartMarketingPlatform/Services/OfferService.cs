using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Offers;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class OfferService(
    MarketingDbContext db,
    IBusinessAccess access,
    StatusCatalog statuses,
    TimeProvider clock) : IOfferService
{
    // Draft -> Active -> Paused <-> Active -> Ended. Ended is final.
    private static readonly StatusFlow Flow = new("offer", new Dictionary<int, int[]>
    {
        [StatusIds.Offer.Draft] = [StatusIds.Offer.Active, StatusIds.Offer.Ended],
        [StatusIds.Offer.Active] = [StatusIds.Offer.Paused, StatusIds.Offer.Ended],
        [StatusIds.Offer.Paused] = [StatusIds.Offer.Active, StatusIds.Offer.Ended],
        [StatusIds.Offer.Ended] = []
    });

    public async Task<PagedResult<OfferResponse>> GetAllAsync(Guid businessId, OfferListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var offers = db.Offers.AsNoTracking().Where(o => o.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("Offer", query.Status);
            offers = offers.Where(o => o.StatusId == statusId);
        }

        var page = await offers
            .OrderByDescending(o => o.StartsAt).ThenBy(o => o.Id)
            .ToPagedResultAsync(query, cancellationToken);

        var now = clock.GetUtcNow();
        return page.Map(o => ToResponse(o, now));
    }

    public async Task<OfferResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken), clock.GetUtcNow());
    }

    public async Task<OfferResponse> CreateAsync(Guid businessId, OfferRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var offer = new Offer { BusinessId = businessId, StatusId = StatusIds.Offer.Draft };
        Apply(offer, request, business.DefaultCurrency);

        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(offer, clock.GetUtcNow());
    }

    public async Task<OfferResponse> UpdateAsync(Guid businessId, Guid id, OfferRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var offer = await LoadAsync(businessId, id, cancellationToken);
        if (offer.StatusId == StatusIds.Offer.Ended)
            throw new ConflictException("An ended offer cannot be changed.");

        Apply(offer, request, business.DefaultCurrency);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(offer, clock.GetUtcNow());
    }

    public async Task<OfferResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var offer = await LoadAsync(businessId, id, cancellationToken);
        var target = statuses.GetId("Offer", request.Status!);

        Flow.EnsureCanMove(offer.StatusId, target, statuses);
        offer.StatusId = target;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(offer, clock.GetUtcNow());
    }

    // Only drafts that no campaign uses can be deleted; anything else is history.
    public async Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var offer = await LoadAsync(businessId, id, cancellationToken);

        if (offer.StatusId != StatusIds.Offer.Draft
            || await db.CampaignOffers.AnyAsync(co => co.OfferId == id, cancellationToken))
            throw new ConflictException("Only a draft offer that no campaign uses can be deleted; end it instead.");

        db.Offers.Remove(offer);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Offer> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.Offers.FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Offer not found.");

    // Rules that need more than one field, so they live here and not in attributes.
    private static void Apply(Offer offer, OfferRequest request, string defaultCurrency)
    {
        if (request.EndsAt <= request.StartsAt)
            throw new BadRequestException("EndsAt must be after StartsAt.");
        if ((request.OfferPrice is null) == (request.DiscountPercent is null))
            throw new BadRequestException("Send either offerPrice or discountPercent (exactly one).");

        offer.Title = request.Title!.Trim();
        offer.Description = request.Description?.Trim();
        offer.OfferPrice = request.OfferPrice;
        offer.Currency = request.OfferPrice is null ? null : request.Currency ?? defaultCurrency;
        offer.DiscountPercent = request.DiscountPercent;
        offer.StartsAt = request.StartsAt!.Value;
        offer.EndsAt = request.EndsAt!.Value;
    }

    private OfferResponse ToResponse(Offer o, DateTimeOffset now) => new(
        o.Id, o.BusinessId, o.Title, o.Description, o.OfferPrice, o.Currency, o.DiscountPercent,
        o.StartsAt, o.EndsAt, statuses.Describe(o.StatusId),
        o.StatusId == StatusIds.Offer.Active && o.StartsAt <= now && now < o.EndsAt,
        o.CreatedAt);
}
