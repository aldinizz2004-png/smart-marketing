namespace SmartMarketingPlatform.Services;

// The work the background worker does on each tick. Separate from the worker
// so it can be called (and tested) without waiting for the timer.
public interface IPublicationProcessor
{
    Task<int> PublishDueJobsAsync(CancellationToken cancellationToken);
    Task<int> RecoverStuckJobsAsync(CancellationToken cancellationToken);
    Task<int> CollectMetricsAsync(Guid? onlyPublicationId, CancellationToken cancellationToken);
}
