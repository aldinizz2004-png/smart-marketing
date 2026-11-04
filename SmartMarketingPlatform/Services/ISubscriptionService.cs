using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Subscriptions;

namespace SmartMarketingPlatform.Services;

// Subscriptions of the current tenant. History is never overwritten:
// every period (and every renewal) is its own row.
public interface ISubscriptionService
{
    Task<PagedResult<SubscriptionResponse>> GetHistoryAsync(PageQuery page, CancellationToken cancellationToken);
    Task<SubscriptionResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SubscriptionResponse?> FindCurrentAsync(CancellationToken cancellationToken);

    Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken);
    Task<SubscriptionResponse> ActivateAsync(Guid id, ActivateSubscriptionRequest request, CancellationToken cancellationToken);
    Task<SubscriptionResponse> CancelAsync(Guid id, CancellationToken cancellationToken);
}
