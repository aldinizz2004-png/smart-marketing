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
using SmartMarketingPlatform.DTOs.Campaigns;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Life cycle: Draft -> (AI plan) -> Ready -> Active <-> Paused -> Completed. Canceled from anywhere open.
// "Planning" is reserved for when plans are generated in the background (later phase).
public sealed class CampaignService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    StatusCatalog statuses,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    TimeProvider clock) : ICampaignService
{
    private static readonly StatusFlow Flow = new("campaign", new Dictionary<int, int[]>
    {
        [StatusIds.Campaign.Draft] = [StatusIds.Campaign.Ready, StatusIds.Campaign.Canceled],
        [StatusIds.Campaign.Planning] = [StatusIds.Campaign.Ready, StatusIds.Campaign.Draft, StatusIds.Campaign.Canceled],
        [StatusIds.Campaign.Ready] = [StatusIds.Campaign.Draft, StatusIds.Campaign.Active, StatusIds.Campaign.Canceled],
        [StatusIds.Campaign.Active] = [StatusIds.Campaign.Paused, StatusIds.Campaign.Completed, StatusIds.Campaign.Canceled],
        [StatusIds.Campaign.Paused] = [StatusIds.Campaign.Active, StatusIds.Campaign.Completed, StatusIds.Campaign.Canceled],
        [StatusIds.Campaign.Completed] = [],
        [StatusIds.Campaign.Canceled] = []
    });

    // Planning and Ready are set by the system (plan generation), not requested by the client.
    private static readonly int[] ClientTargets =
    [
        StatusIds.Campaign.Draft, StatusIds.Campaign.Active, StatusIds.Campaign.Paused,
        StatusIds.Campaign.Completed, StatusIds.Campaign.Canceled
    ];

    private static readonly int[] Editable = [StatusIds.Campaign.Draft, StatusIds.Campaign.Ready];

    public async Task<PagedResult<CampaignResponse>> GetAllAsync(Guid businessId, CampaignListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var campaigns = WithLinks().AsNoTracking().Where(c => c.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("Campaign", query.Status);
            campaigns = campaigns.Where(c => c.StatusId == statusId);
        }
        if (query.Objective is not null)
            campaigns = campaigns.Where(c => c.Objective == query.Objective);

        var page = await campaigns
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .ToPagedResultAsync(query, cancellationToken);

        // Latest plan version of the campaigns on this page, in one query.
        var ids = page.Items.Select(c => c.Id).ToList();
        var versions = await db.CampaignPlans
            .Where(p => ids.Contains(p.CampaignId))
            .GroupBy(p => p.CampaignId)
            .Select(g => new { CampaignId = g.Key, Version = g.Max(p => p.VersionNo) })
            .ToDictionaryAsync(x => x.CampaignId, x => x.Version, cancellationToken);

        return page.Map(c => ToResponse(c, versions.TryGetValue(c.Id, out var v) ? v : null));
    }

    public async Task<CampaignResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var campaign = await LoadAsync(businessId, id, cancellationToken);
        return ToResponse(campaign, await LatestVersionAsync(id, cancellationToken));
    }

    public async Task<CampaignResponse> CreateAsync(Guid businessId, CampaignRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        await ValidateAsync(business, request, cancellationToken);

        var campaign = new Campaign
        {
            BusinessId = businessId,
            CreatedBy = currentUser.UserId,
            StatusId = StatusIds.Campaign.Draft
        };
        Apply(campaign, request, business.DefaultCurrency);
        SyncLinks(campaign, request);

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(campaign, null);
    }

    public async Task<CampaignResponse> UpdateAsync(Guid businessId, Guid id, CampaignRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var campaign = await LoadAsync(businessId, id, cancellationToken);
        if (!Editable.Contains(campaign.StatusId))
            throw new ConflictException($"A {statuses.CodeOf(campaign.StatusId)} campaign cannot be edited; pause or cancel it instead.");

        await ValidateAsync(business, request, cancellationToken);
        Apply(campaign, request, business.DefaultCurrency);
        SyncLinks(campaign, request);

        // The plan was made for the old goal: the campaign needs a new plan before it can run.
        if (campaign.StatusId == StatusIds.Campaign.Ready)
            campaign.StatusId = StatusIds.Campaign.Draft;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(campaign, await LatestVersionAsync(id, cancellationToken));
    }

    public async Task<CampaignResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var campaign = await LoadAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("Campaign", request.Status!);
        if (!ClientTargets.Contains(target))
            throw new BadRequestException("Planning and Ready are set by plan generation (POST .../plans).");

        // Starting a campaign commits the owner's budget: Owner or Admin only.
        if (target == StatusIds.Campaign.Active && currentUser.Role is not (MembershipRole.Owner or MembershipRole.Admin))
            throw new ForbiddenException("Only the owner or an admin can start a campaign.");

        Flow.EnsureCanMove(campaign.StatusId, target, statuses);
        campaign.StatusId = target;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(campaign, await LatestVersionAsync(id, cancellationToken));
    }

    public async Task<CampaignPlanResponse> GeneratePlanAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var campaign = await LoadAsync(businessId, id, cancellationToken);
        if (!Editable.Contains(campaign.StatusId))
            throw new ConflictException("Plans can only be generated for Draft or Ready campaigns.");

        var context = await contextBuilder.BuildAsync(businessId, id, cancellationToken);
        var now = clock.GetUtcNow();

        // Recorded as an AiExecution (+ UsageEvent). A provider failure becomes a 502.
        var (plan, execution) = await aiRunner.RunAsync(
            businessId, AiTaskType.Strategy, UsageEventType.StrategyGeneration, context,
            ct => ai.PlanCampaignAsync(context, now, ct), cancellationToken);

        var nextVersion = (await LatestVersionAsync(id, cancellationToken) ?? 0) + 1;
        var entity = new CampaignPlan
        {
            CampaignId = id,
            VersionNo = nextVersion,                // unique (CampaignId, VersionNo): a parallel request gets 409
            PlanJson = plan.ToJsonString(),
            GeneratedByExecutionId = execution.Id
        };
        db.CampaignPlans.Add(entity);
        campaign.StatusId = StatusIds.Campaign.Ready;

        await db.SaveChangesAsync(cancellationToken);
        return ToPlanResponse(entity);
    }

    public async Task<IReadOnlyList<CampaignPlanResponse>> GetPlansAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        await LoadAsync(businessId, id, cancellationToken);
        var plans = await db.CampaignPlans.AsNoTracking()
            .Where(p => p.CampaignId == id)
            .OrderByDescending(p => p.VersionNo)
            .ToListAsync(cancellationToken);
        return plans.Select(ToPlanResponse).ToList();
    }

    public async Task<CampaignPlanResponse> GetPlanAsync(Guid businessId, Guid id, int versionNo, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        await LoadAsync(businessId, id, cancellationToken);
        var plan = await db.CampaignPlans.AsNoTracking().FirstOrDefaultAsync(p => p.CampaignId == id && p.VersionNo == versionNo, cancellationToken)
                   ?? throw new NotFoundException($"Plan version {versionNo} not found.");
        return ToPlanResponse(plan);
    }

    // ---- rules ----------------------------------------------------------------

    private async Task ValidateAsync(Business business, CampaignRequest request, CancellationToken cancellationToken)
    {
        if (request.StartsAt is not null && request.EndsAt is not null && request.EndsAt <= request.StartsAt)
            throw new BadRequestException("EndsAt must be after StartsAt.");
        if (request.TargetValue is not null && string.IsNullOrWhiteSpace(request.TargetMetric))
            throw new BadRequestException("targetValue needs targetMetric.");
        if (request.BudgetCurrency is not null && request.Budget is null)
            throw new BadRequestException("budgetCurrency needs budget.");

        // Every linked item must belong to this business.
        await EnsureAllExistAsync("product", request.ProductIds,
            ids => db.Products.CountAsync(p => p.BusinessId == business.Id && ids.Contains(p.Id), cancellationToken));
        await EnsureAllExistAsync("offer", request.OfferIds,
            ids => db.Offers.CountAsync(o => o.BusinessId == business.Id && ids.Contains(o.Id) && o.StatusId != StatusIds.Offer.Ended, cancellationToken));
        await EnsureAllExistAsync("audience segment", request.AudienceSegmentIds,
            ids => db.AudienceSegments.CountAsync(a => a.BusinessId == business.Id && ids.Contains(a.Id), cancellationToken));

        // The owner's limit: one campaign cannot exceed the monthly ad budget of the automation policy.
        var limit = await db.AutomationPolicies.Where(p => p.BusinessId == business.Id)
            .Select(p => p.MonthlyAdBudget).FirstOrDefaultAsync(cancellationToken);
        if (limit is not null && request.Budget > limit)
            throw new BadRequestException($"The budget is above the owner's monthly ad budget ({limit}).");
    }

    private static async Task EnsureAllExistAsync(string name, List<Guid> requested, Func<List<Guid>, Task<int>> countFound)
    {
        var ids = requested.Distinct().ToList();
        if (ids.Count > 0 && await countFound(ids) != ids.Count)
            throw new BadRequestException($"One or more {name} ids are unknown for this business (or no longer usable).");
    }

    private static void Apply(Campaign campaign, CampaignRequest request, string defaultCurrency)
    {
        campaign.Name = request.Name!.Trim();
        campaign.Objective = request.Objective!.Value;
        campaign.GoalStatement = request.GoalStatement!.Trim();
        campaign.TargetMetric = request.TargetMetric?.Trim();
        campaign.TargetValue = request.TargetValue;
        campaign.Budget = request.Budget;
        campaign.BudgetCurrency = request.Budget is null ? null : request.BudgetCurrency ?? defaultCurrency;
        campaign.StartsAt = request.StartsAt;
        campaign.EndsAt = request.EndsAt;
    }

    // Join tables are synced by difference (same reason as the automation policy lists).
    private static void SyncLinks(Campaign campaign, CampaignRequest request)
    {
        var products = request.ProductIds.Distinct().ToList();
        foreach (var gone in campaign.CampaignProducts.Where(x => !products.Contains(x.ProductId)).ToList())
            campaign.CampaignProducts.Remove(gone);
        foreach (var id in products.Where(id => campaign.CampaignProducts.All(x => x.ProductId != id)))
            campaign.CampaignProducts.Add(new CampaignProduct { CampaignId = campaign.Id, ProductId = id });

        var offers = request.OfferIds.Distinct().ToList();
        foreach (var gone in campaign.CampaignOffers.Where(x => !offers.Contains(x.OfferId)).ToList())
            campaign.CampaignOffers.Remove(gone);
        foreach (var id in offers.Where(id => campaign.CampaignOffers.All(x => x.OfferId != id)))
            campaign.CampaignOffers.Add(new CampaignOffer { CampaignId = campaign.Id, OfferId = id });

        var audiences = request.AudienceSegmentIds.Distinct().ToList();
        foreach (var gone in campaign.CampaignAudiences.Where(x => !audiences.Contains(x.AudienceSegmentId)).ToList())
            campaign.CampaignAudiences.Remove(gone);
        foreach (var id in audiences.Where(id => campaign.CampaignAudiences.All(x => x.AudienceSegmentId != id)))
            campaign.CampaignAudiences.Add(new CampaignAudience { CampaignId = campaign.Id, AudienceSegmentId = id });
    }

    private IQueryable<Campaign> WithLinks() =>
        db.Campaigns.Include(c => c.CampaignProducts).Include(c => c.CampaignOffers).Include(c => c.CampaignAudiences)
            .AsSplitQuery();   // three collections: one SQL query each, instead of one query that multiplies rows

    private async Task<Campaign> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await WithLinks().FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Campaign not found.");

    private Task<int?> LatestVersionAsync(Guid campaignId, CancellationToken cancellationToken) =>
        db.CampaignPlans.Where(p => p.CampaignId == campaignId).MaxAsync(p => (int?)p.VersionNo, cancellationToken);

    private CampaignResponse ToResponse(Campaign c, int? latestPlanVersion) => new(
        c.Id, c.BusinessId, c.Name, c.Objective, c.GoalStatement, c.TargetMetric, c.TargetValue,
        c.Budget, c.BudgetCurrency, c.StartsAt, c.EndsAt, statuses.Describe(c.StatusId), c.CreatedBy,
        c.CampaignProducts.Select(x => x.ProductId).ToList(),
        c.CampaignOffers.Select(x => x.OfferId).ToList(),
        c.CampaignAudiences.Select(x => x.AudienceSegmentId).ToList(),
        latestPlanVersion, c.CreatedAt, c.UpdatedAt);

    private static CampaignPlanResponse ToPlanResponse(CampaignPlan p) => new(
        p.Id, p.CampaignId, p.VersionNo, JsonDocument.Parse(p.PlanJson).RootElement.Clone(), p.GeneratedByExecutionId, p.CreatedAt);
}
