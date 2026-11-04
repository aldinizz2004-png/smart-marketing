using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Strategy;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// A strategy run asks the AI "what should we do in the next N days?".
// Each answer is a recommendation the owner accepts or rejects, unless the owner's
// automation policy allows that kind of action to be accepted automatically.
public sealed class StrategyService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    StatusCatalog statuses,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    TimeProvider clock) : IStrategyService
{
    private static readonly int[] ClosedCampaign = [StatusIds.Campaign.Completed, StatusIds.Campaign.Canceled];
    private static readonly int[] Decisions = [StatusIds.Recommendation.Accepted, StatusIds.Recommendation.Rejected];

    public async Task<StrategyRunResponse> StartRunAsync(Guid businessId, StartStrategyRunRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        if (request.CampaignId is { } campaignId)
        {
            var campaignStatus = await db.Campaigns
                .Where(c => c.Id == campaignId && c.BusinessId == businessId)
                .Select(c => (int?)c.StatusId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Campaign not found.");
            if (ClosedCampaign.Contains(campaignStatus))
                throw new ConflictException("The campaign is completed or canceled.");
        }

        var now = clock.GetUtcNow();
        var context = await contextBuilder.BuildAsync(businessId, request.CampaignId, cancellationToken);

        var run = new StrategyRun
        {
            BusinessId = businessId,
            WindowStart = now,
            WindowEnd = now.AddDays(request.WindowDays),
            // The prompt/algorithm version only: with a fallback chain the model that answers is known
            // after the call and is recorded on the linked AiExecution (ModelName).
            AlgorithmVersion = ai.PromptVersion,
            StatusId = StatusIds.StrategyRun.Running,
            InputSummaryJson = JsonSerializer.Serialize(new
            {
                request.CampaignId,
                request.WindowDays,
                products = context.Products.Count,
                offers = context.Offers.Count,
                audiences = context.Audiences.Count,
                context.Channels
            }),
            StartedAt = now
        };
        db.StrategyRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        IReadOnlyList<RecommendationDraft> drafts;
        AiExecution execution;
        try
        {
            (drafts, execution) = await aiRunner.RunAsync(
                businessId, AiTaskType.Strategy, UsageEventType.StrategyGeneration, context,
                ct => ai.RecommendAsync(context, run.WindowStart, run.WindowEnd, ct), cancellationToken);
        }
        catch (AppException)
        {
            run.StatusId = StatusIds.StrategyRun.Failed;
            run.CompletedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        // The owner's policy decides what may be accepted without asking.
        var policy = await db.AutomationPolicies.AsNoTracking().Include(p => p.AutoApplyRules)
            .FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);
        var autoActions = policy is { AutomationEnabled: true }
            ? policy.AutoApplyRules.Select(r => r.ActionType).ToHashSet()
            : [];

        var decidedAt = clock.GetUtcNow();
        foreach (var draft in drafts)
        {
            var automatic = autoActions.Contains(draft.ActionType);
            run.Recommendations.Add(new StrategyRecommendation
            {
                CampaignId = request.CampaignId,
                ActionType = draft.ActionType,
                ProposedPayloadJson = draft.Payload.ToJsonString(),
                ProposedPublishAt = draft.ProposedPublishAt,
                Rationale = draft.Rationale,
                Confidence = draft.Confidence,
                StatusId = automatic ? StatusIds.Recommendation.Accepted : StatusIds.Recommendation.Pending,
                AppliedAutomatically = automatic,
                DecidedAt = automatic ? decidedAt : null
            });
        }

        run.AiExecutionId = execution.Id;
        run.StatusId = StatusIds.StrategyRun.Completed;
        run.CompletedAt = decidedAt;

        await db.SaveChangesAsync(cancellationToken);
        return ToRunResponse(run);
    }

    public async Task<PagedResult<StrategyRunResponse>> GetRunsAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var page = await db.StrategyRuns.AsNoTracking()
            .Include(r => r.Recommendations)
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.StartedAt).ThenByDescending(r => r.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToRunResponse);
    }

    public async Task<StrategyRunResponse> GetRunAsync(Guid businessId, Guid runId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var run = await db.StrategyRuns.AsNoTracking().Include(r => r.Recommendations)
                      .FirstOrDefaultAsync(r => r.Id == runId && r.BusinessId == businessId, cancellationToken)
                  ?? throw new NotFoundException("Strategy run not found.");
        return ToRunResponse(run);
    }

    public async Task<PagedResult<RecommendationResponse>> GetRecommendationsAsync(Guid businessId, RecommendationListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var recommendations = Recommendations(businessId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("Recommendation", query.Status);
            recommendations = recommendations.Where(r => r.StatusId == statusId);
        }
        if (query.CampaignId is not null)
            recommendations = recommendations.Where(r => r.CampaignId == query.CampaignId);

        var page = await recommendations
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<RecommendationResponse> GetRecommendationAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadRecommendationAsync(businessId, id, cancellationToken));
    }

    // PATCH .../status { "status": "Accepted" | "Rejected" }. Applied is set by the system later.
    public async Task<RecommendationResponse> DecideAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var recommendation = await LoadRecommendationAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("Recommendation", request.Status!);
        if (!Decisions.Contains(target))
            throw new BadRequestException("A recommendation can only be 'Accepted' or 'Rejected'.");
        if (recommendation.StatusId != StatusIds.Recommendation.Pending)
            throw new ConflictException($"This recommendation was already decided ({statuses.CodeOf(recommendation.StatusId)}).");

        recommendation.StatusId = target;
        recommendation.DecidedBy = currentUser.UserId;
        recommendation.DecidedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(recommendation);
    }

    // A recommendation belongs to a business through its strategy run.
    private IQueryable<StrategyRecommendation> Recommendations(Guid businessId) =>
        db.StrategyRecommendations.Where(r => r.StrategyRun.BusinessId == businessId);

    private async Task<StrategyRecommendation> LoadRecommendationAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await Recommendations(businessId).FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw new NotFoundException("Recommendation not found.");

    private StrategyRunResponse ToRunResponse(StrategyRun r) => new(
        r.Id, r.BusinessId, r.WindowStart, r.WindowEnd, r.AlgorithmVersion, statuses.Describe(r.StatusId),
        r.AiExecutionId, r.StartedAt, r.CompletedAt,
        r.Recommendations.OrderByDescending(x => x.Confidence).Select(ToResponse).ToList());

    private RecommendationResponse ToResponse(StrategyRecommendation r) => new(
        r.Id, r.StrategyRunId, r.CampaignId, r.ActionType,
        JsonDocument.Parse(r.ProposedPayloadJson).RootElement.Clone(),
        r.ProposedPublishAt, r.Rationale, r.Confidence, statuses.Describe(r.StatusId),
        r.AppliedAutomatically, r.DecidedBy, r.DecidedAt, r.AppliedAt, r.CreatedAt);
}
