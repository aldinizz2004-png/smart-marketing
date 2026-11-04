using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Subscriptions;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

// Life cycle: Pending (created, waiting for payment) -> Active (paid) ; Pending -> Canceled.
// A renewal is created while a period is still running and starts when that period ends.
// Access is decided by time (Subscription.GrantsAccessAt), so no job is needed to "start" a renewal.
public sealed class SubscriptionService(
    MarketingDbContext db,
    ICurrentUser currentUser,
    StatusCatalog statuses,
    IOptions<SubscriptionPricingOptions> pricing,
    TimeProvider clock) : ISubscriptionService
{
    public async Task<PagedResult<SubscriptionResponse>> GetHistoryAsync(PageQuery page, CancellationToken cancellationToken)
    {
        var result = await TenantSubscriptions().AsNoTracking()
            .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
            .ToPagedResultAsync(page, cancellationToken);

        var now = clock.GetUtcNow();
        return result.Map(s => ToResponse(s, now));
    }

    public async Task<SubscriptionResponse> GetAsync(Guid id, CancellationToken cancellationToken)
        => ToResponse(await LoadAsync(id, cancellationToken), clock.GetUtcNow());

    public async Task<SubscriptionResponse?> FindCurrentAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var current = await TenantSubscriptions().AsNoTracking()
            .Where(s => s.StatusId == StatusIds.Subscription.Active && s.StartsAt <= now && s.EndsAt > now)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

        return current is null ? null : ToResponse(current, now);
    }

    public async Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var duration = request.Duration!.Value;
        if (!pricing.Value.Prices.TryGetValue(duration, out var price))
            throw new BadRequestException($"No price is configured for {duration}.");

        if (await TenantSubscriptions().AnyAsync(s => s.StatusId == StatusIds.Subscription.Pending, cancellationToken))
            throw new ConflictException("A subscription is already awaiting payment. Activate or cancel it first.");

        // If a paid period is still running (or waiting to start), the new one is its renewal.
        var now = clock.GetUtcNow();
        var latestPaid = await TenantSubscriptions()
            .Where(s => s.StatusId == StatusIds.Subscription.Active && s.EndsAt > now)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

        var startsAt = latestPaid?.EndsAt ?? now;   // final dates for a fresh one are set on activation
        var subscription = new Subscription
        {
            TenantId = currentUser.TenantId,
            Duration = duration,
            Price = price,
            Currency = pricing.Value.Currency,
            StartsAt = startsAt,
            EndsAt = Subscription.CalculateEndsAt(startsAt, duration),
            StatusId = StatusIds.Subscription.Pending,
            RenewedFromSubscriptionId = latestPaid?.Id
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(subscription, now);
    }

    public async Task<SubscriptionResponse> ActivateAsync(Guid id, ActivateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await LoadAsync(id, cancellationToken);
        if (subscription.StatusId != StatusIds.Subscription.Pending)
            throw new ConflictException("Only a subscription that is awaiting payment can be activated.");

        var reference = request.PaymentReference!.Trim();
        if (await db.Subscriptions.AnyAsync(s => s.PaymentReference == reference, cancellationToken))
            throw new ConflictException("This payment reference was already used.");

        // A renewal starts when the period it renews ends (or now, if that already passed).
        // A fresh subscription starts at the moment of payment.
        var now = clock.GetUtcNow();
        var startsAt = now;
        if (subscription.RenewedFromSubscriptionId is { } previousId)
        {
            var previousEndsAt = await db.Subscriptions
                .Where(s => s.Id == previousId)
                .Select(s => s.EndsAt)
                .FirstAsync(cancellationToken);
            if (previousEndsAt > now) startsAt = previousEndsAt;
        }

        subscription.StartsAt = startsAt;
        subscription.EndsAt = Subscription.CalculateEndsAt(startsAt, subscription.Duration);
        subscription.PaymentReference = reference;
        subscription.StatusId = StatusIds.Subscription.Active;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(subscription, now);
    }

    public async Task<SubscriptionResponse> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await LoadAsync(id, cancellationToken);

        // A paid period is cancelled through a refund with the payment provider, not here.
        if (subscription.StatusId != StatusIds.Subscription.Pending)
            throw new ConflictException("Only a subscription that is awaiting payment can be canceled.");

        var now = clock.GetUtcNow();
        subscription.StatusId = StatusIds.Subscription.Canceled;
        subscription.CanceledAt = now;
        // It was never paid, so it is not a real renewal: free the period for a new renewal
        // (the unique index allows only one renewal per period).
        subscription.RenewedFromSubscriptionId = null;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(subscription, now);
    }

    // Every query starts from the token's tenant.
    private IQueryable<Subscription> TenantSubscriptions() =>
        db.Subscriptions.Where(s => s.TenantId == currentUser.TenantId);

    private async Task<Subscription> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await TenantSubscriptions().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Subscription not found.");

    private SubscriptionResponse ToResponse(Subscription s, DateTimeOffset now) => new(
        s.Id,
        s.Duration,
        s.Price,
        s.Currency,
        s.StartsAt,
        s.EndsAt,
        statuses.Describe(s.StatusId),
        s.RenewedFromSubscriptionId,
        s.CanceledAt,
        s.GrantsAccessAt(now),
        s.CreatedAt);
}
