using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Usage;

namespace SmartMarketingPlatform.Services;

public sealed class AiUsageService(
    MarketingDbContext db,
    ICurrentUser currentUser,
    IBusinessAccess access,
    StatusCatalog statuses,
    TimeProvider clock) : IAiUsageService
{
    public async Task<AiUsageResponse> GetUsageAsync(AiUsageQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var from = query.From ?? new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = query.To ?? now;
        if (from >= to)
            throw new BadRequestException("'from' must be before 'to'.");

        var tenantId = currentUser.TenantId;
        var usage = db.UsageEvents.AsNoTracking().Where(u => u.TenantId == tenantId && u.OccurredAt >= from && u.OccurredAt < to);

        var byType = await usage
            .GroupBy(u => u.EventType)
            .Select(g => new UsageByType(g.Key, g.Count(), g.Sum(u => u.Units), g.Sum(u => u.EstimatedCostUsd)))
            .ToListAsync(cancellationToken);

        var byBusiness = await usage
            .Where(u => u.BusinessId != null)
            .GroupBy(u => new { u.BusinessId, u.Business!.Name })
            .Select(g => new UsageByBusiness(g.Key.BusinessId!.Value, g.Key.Name, g.Count(), g.Sum(u => u.EstimatedCostUsd)))
            .ToListAsync(cancellationToken);

        // Executions include the failed ones (no usage event, but worth seeing).
        var executions = db.AiExecutions.AsNoTracking()
            .Where(e => e.Business.TenantId == tenantId && e.CreatedAt >= from && e.CreatedAt < to);
        var stats = new AiExecutionStats(
            await executions.CountAsync(cancellationToken),
            await executions.CountAsync(e => e.StatusId == StatusIds.AiExecution.Succeeded, cancellationToken),
            await executions.CountAsync(e => e.StatusId == StatusIds.AiExecution.Failed, cancellationToken),
            (int?)await executions.Where(e => e.LatencyMs != null).AverageAsync(e => (double?)e.LatencyMs, cancellationToken));

        return new AiUsageResponse(from, to, byType.Sum(t => t.CostUsd), stats,
            byType.OrderByDescending(t => t.CostUsd).ToList(),
            byBusiness.OrderByDescending(b => b.CostUsd).ToList());
    }

    public async Task<AiExecutionResponse> GetExecutionAsync(Guid businessId, Guid executionId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var e = await db.AiExecutions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == executionId && x.BusinessId == businessId, cancellationToken)
                ?? throw new NotFoundException("AI execution not found.");

        return new AiExecutionResponse(
            e.Id, e.BusinessId, e.TaskType, e.ModelName, e.PromptVersion, statuses.Describe(e.StatusId),
            Parse(e.InputSnapshotJson), Parse(e.OutputSnapshotJson),
            e.InputTokens, e.OutputTokens, e.EstimatedCostUsd, e.LatencyMs, e.ErrorCode, e.ErrorMessage,
            e.StartedAt, e.CompletedAt);
    }

    private static JsonElement? Parse(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();
}
