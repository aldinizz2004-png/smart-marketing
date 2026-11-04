using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

public sealed class AiExecutionRunner(
    MarketingDbContext db,
    IMarketingAi ai,
    IOptions<AiOptions> options,
    TimeProvider clock,
    ILogger<AiExecutionRunner> logger) : IAiExecutionRunner
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<(T Output, AiExecution Execution)> RunAsync<T>(
        Guid businessId,
        AiTaskType taskType,
        UsageEventType usageType,
        object inputSnapshot,
        Func<CancellationToken, Task<AiCallResult<T>>> call,
        CancellationToken cancellationToken)
    {
        // 1) Saved as Running BEFORE the call: if the process dies, the attempt is still visible.
        var execution = new AiExecution
        {
            BusinessId = businessId,
            TaskType = taskType,
            ModelName = Truncate(ai.ModelName, 100),
            PromptVersion = ai.PromptVersion,
            StatusId = StatusIds.AiExecution.Running,
            InputSnapshotJson = JsonSerializer.Serialize(inputSnapshot, Json),
            StartedAt = clock.GetUtcNow()
        };
        db.AiExecutions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        AiCallResult<T> result;
        try
        {
            result = await call(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 2a) Failure: recorded, then reported as 502 (provider failure) or rethrown (our bug -> 500).
            execution.StatusId = StatusIds.AiExecution.Failed;
            execution.ErrorCode = ex is AiProviderException p ? p.Code : "unexpected_error";
            execution.ErrorMessage = Truncate(ex.Message, 2000);
            execution.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
            execution.CompletedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(CancellationToken.None);

            logger.LogWarning(ex, "AI execution {ExecutionId} ({TaskType}) failed for business {BusinessId}",
                execution.Id, taskType, businessId);

            if (ex is AiProviderException)
                throw new BadGatewayException($"The AI service failed ({execution.ErrorCode}). Execution {execution.Id} was recorded.");
            throw;
        }

        // 2b) Success: tokens, cost and a usage event. Saved right away: the money was spent
        //     even if the caller fails afterwards. The idempotency key stops double counting.
        var cost = result.CostUsd
                   ?? result.InputTokens / 1_000_000m * options.Value.InputCostPer1MTokensUsd
                   + result.OutputTokens / 1_000_000m * options.Value.OutputCostPer1MTokensUsd;

        // With a fallback chain, the model that really answered is known only now.
        if (!string.IsNullOrWhiteSpace(result.ModelName))
            execution.ModelName = Truncate(result.ModelName, 100);
        // Not an error of this execution, but worth seeing: which models failed before this one answered.
        if (result.FailedAttempts.Count > 0)
            execution.ErrorMessage = Truncate("Answered after fallback. " + string.Join("; ", result.FailedAttempts), 2000);

        execution.StatusId = StatusIds.AiExecution.Succeeded;
        execution.OutputSnapshotJson = JsonSerializer.Serialize(result.Output, Json);
        execution.InputTokens = result.InputTokens;
        execution.OutputTokens = result.OutputTokens;
        execution.EstimatedCostUsd = Math.Round(cost, 6);
        execution.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        execution.CompletedAt = clock.GetUtcNow();

        // The tenant is read from the business, so background jobs (weekly reports) are billed too.
        var tenantId = await db.Businesses.Where(b => b.Id == businessId).Select(b => b.TenantId).FirstAsync(cancellationToken);
        db.UsageEvents.Add(new UsageEvent
        {
            TenantId = tenantId,
            BusinessId = businessId,
            AiExecutionId = execution.Id,
            EventType = usageType,
            Units = result.InputTokens + result.OutputTokens,
            EstimatedCostUsd = execution.EstimatedCostUsd.Value,
            IdempotencyKey = $"ai-execution:{execution.Id:N}",
            OccurredAt = execution.CompletedAt.Value
        });
        await db.SaveChangesAsync(cancellationToken);

        return (result.Output, execution);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
