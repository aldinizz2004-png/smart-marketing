using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Publishing;

namespace SmartMarketingPlatform.Services;

public interface IPublishingService
{
    Task<PublicationJobResponse> ScheduleAsync(Guid businessId, Guid contentItemId, ScheduleRequest request, CancellationToken cancellationToken);

    // Called after an approval: schedules by itself only if the owner's automation policy allows it.
    Task<bool> TryAutoScheduleAsync(Guid businessId, Guid contentItemId, CancellationToken cancellationToken);

    Task<PublicationJobResponse> CancelJobAsync(Guid businessId, Guid jobId, CancellationToken cancellationToken);
    Task<int> CancelActiveJobsAsync(Guid contentItemId, CancellationToken cancellationToken);

    Task<PagedResult<PublicationJobResponse>> GetJobsAsync(Guid businessId, JobListQuery query, CancellationToken cancellationToken);
    Task<PublicationJobResponse> GetJobAsync(Guid businessId, Guid jobId, CancellationToken cancellationToken);
}
