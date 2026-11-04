using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// Wraps every AI call: records an AiExecution (input, output, tokens, cost, latency, errors)
// and a UsageEvent for billing. Services never call IMarketingAi directly.
public interface IAiExecutionRunner
{
    Task<(T Output, AiExecution Execution)> RunAsync<T>(
        Guid businessId,
        AiTaskType taskType,
        UsageEventType usageType,
        object inputSnapshot,
        Func<CancellationToken, Task<AiCallResult<T>>> call,
        CancellationToken cancellationToken);
}
