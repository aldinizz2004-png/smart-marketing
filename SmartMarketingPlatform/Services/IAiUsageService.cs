using SmartMarketingPlatform.DTOs.Usage;

namespace SmartMarketingPlatform.Services;

public interface IAiUsageService
{
    Task<AiUsageResponse> GetUsageAsync(AiUsageQuery query, CancellationToken cancellationToken);
    Task<AiExecutionResponse> GetExecutionAsync(Guid businessId, Guid executionId, CancellationToken cancellationToken);
}
