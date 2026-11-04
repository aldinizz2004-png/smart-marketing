using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Automation;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class AutomationPolicyService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser) : IAutomationPolicyService
{
    private const string TimeFormat = "HH:mm";

    public async Task<AutomationPolicyResponse> GetAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var policy = await LoadAsync(businessId, cancellationToken);
        return policy is null ? Defaults(businessId) : ToResponse(policy);
    }

    public async Task<(AutomationPolicyResponse Policy, bool Created)> UpsertAsync(
        Guid businessId, AutomationPolicyRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);

        // ---- rules that involve several fields ----
        if ((request.PublishWindowStart is null) != (request.PublishWindowEnd is null))
            throw new BadRequestException("Send both publishWindowStart and publishWindowEnd, or neither.");
        TimeOnly? start = request.PublishWindowStart is null ? null : TimeOnly.ParseExact(request.PublishWindowStart, TimeFormat, CultureInfo.InvariantCulture);
        TimeOnly? end = request.PublishWindowEnd is null ? null : TimeOnly.ParseExact(request.PublishWindowEnd, TimeFormat, CultureInfo.InvariantCulture);
        if (start >= end)
            throw new BadRequestException("publishWindowStart must be before publishWindowEnd.");
        if (request.BudgetCurrency is not null && request.MonthlyAdBudget is null)
            throw new BadRequestException("budgetCurrency needs monthlyAdBudget.");

        var platformCodes = request.AllowedPlatforms
            .Select(c => (c ?? string.Empty).Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        var publishable = await db.Platforms.AsNoTracking()
            .Where(p => platformCodes.Contains(p.Code) && p.IsActive && p.SupportsPublishing)
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);
        var invalid = platformCodes.Except(publishable).ToList();
        if (invalid.Count > 0)
            throw new BadRequestException($"Not a publishing platform: {string.Join(", ", invalid)}. See /api/platforms.");

        var actions = request.AutoApplyActions.Distinct().ToList();

        // ---- create or replace ----
        var policy = await LoadAsync(businessId, cancellationToken);
        var created = policy is null;
        if (policy is null)
        {
            policy = new AutomationPolicy { BusinessId = businessId };
            db.AutomationPolicies.Add(policy);
        }

        policy.AutomationEnabled = request.AutomationEnabled!.Value;
        policy.MaxPostsPerDay = request.MaxPostsPerDay!.Value;
        policy.PublishWindowStart = start;
        policy.PublishWindowEnd = end;
        policy.MaxDiscountPercent = request.MaxDiscountPercent;
        policy.MonthlyAdBudget = request.MonthlyAdBudget;
        policy.BudgetCurrency = request.MonthlyAdBudget is null ? null : request.BudgetCurrency ?? business.DefaultCurrency;
        policy.RequireSeparateReviewer = request.RequireSeparateReviewer;
        policy.UpdatedBy = currentUser.UserId;
        if (!created)
            db.Entry(policy).Property(p => p.UpdatedBy).IsModified = true;   // a save always counts as an update (moves UpdatedAt)

        // Sync the two lists by difference (remove what is gone, add what is new):
        // removing and re-adding the same composite key in one SaveChanges would clash.
        foreach (var gone in policy.AllowedPlatforms.Where(p => !platformCodes.Contains(p.PlatformCode)).ToList())
            policy.AllowedPlatforms.Remove(gone);
        foreach (var code in platformCodes.Where(c => policy.AllowedPlatforms.All(p => p.PlatformCode != c)))
            policy.AllowedPlatforms.Add(new AutomationPolicyPlatform { BusinessId = businessId, PlatformCode = code });

        foreach (var gone in policy.AutoApplyRules.Where(r => !actions.Contains(r.ActionType)).ToList())
            policy.AutoApplyRules.Remove(gone);
        foreach (var action in actions.Where(a => policy.AutoApplyRules.All(r => r.ActionType != a)))
            policy.AutoApplyRules.Add(new AutoApplyRule { BusinessId = businessId, ActionType = action });

        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(policy), created);
    }

    private Task<AutomationPolicy?> LoadAsync(Guid businessId, CancellationToken cancellationToken) =>
        db.AutomationPolicies
            .Include(p => p.AllowedPlatforms)
            .Include(p => p.AutoApplyRules)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.BusinessId == businessId, cancellationToken);

    // Never configured = automation off: nothing is published without the owner.
    private static AutomationPolicyResponse Defaults(Guid businessId) => new(
        businessId, false, false, 3, null, null, null, null, null, false, [], [], null, null);

    private static AutomationPolicyResponse ToResponse(AutomationPolicy p) => new(
        p.BusinessId,
        true,
        p.AutomationEnabled,
        p.MaxPostsPerDay,
        p.PublishWindowStart?.ToString(TimeFormat, CultureInfo.InvariantCulture),
        p.PublishWindowEnd?.ToString(TimeFormat, CultureInfo.InvariantCulture),
        p.MaxDiscountPercent,
        p.MonthlyAdBudget,
        p.BudgetCurrency,
        p.RequireSeparateReviewer,
        p.AllowedPlatforms.Select(x => x.PlatformCode).OrderBy(x => x).ToList(),
        p.AutoApplyRules.Select(x => x.ActionType).OrderBy(x => x).ToList(),
        p.UpdatedBy,
        p.UpdatedAt);
}
