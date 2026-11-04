using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Audience;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class AudienceSegmentService(MarketingDbContext db, IBusinessAccess access) : IAudienceSegmentService
{
    private const int MaxRulesLength = 4000;

    public async Task<PagedResult<AudienceSegmentResponse>> GetAllAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var page = await db.AudienceSegments.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderBy(a => a.Name).ThenBy(a => a.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<AudienceSegmentResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken));
    }

    public async Task<AudienceSegmentResponse> CreateAsync(Guid businessId, AudienceSegmentRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        await EnsureNameIsFreeAsync(businessId, request.Name!, null, cancellationToken);

        var segment = new AudienceSegment { BusinessId = businessId };
        Apply(segment, request);
        db.AudienceSegments.Add(segment);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(segment);
    }

    public async Task<AudienceSegmentResponse> UpdateAsync(Guid businessId, Guid id, AudienceSegmentRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var segment = await LoadAsync(businessId, id, cancellationToken);
        await EnsureNameIsFreeAsync(businessId, request.Name!, id, cancellationToken);

        Apply(segment, request);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(segment);
    }

    public async Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var segment = await LoadAsync(businessId, id, cancellationToken);

        if (await db.CampaignAudiences.AnyAsync(ca => ca.AudienceSegmentId == id, cancellationToken))
            throw new ConflictException("This audience is used by a campaign and cannot be deleted.");

        db.AudienceSegments.Remove(segment);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsFreeAsync(Guid businessId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var value = name.Trim();
        if (await db.AudienceSegments.AnyAsync(a => a.BusinessId == businessId && a.Name == value && a.Id != exceptId, cancellationToken))
            throw new ConflictException($"An audience named '{value}' already exists for this business.");
    }

    private async Task<AudienceSegment> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.AudienceSegments.FirstOrDefaultAsync(a => a.Id == id && a.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Audience segment not found.");

    private static void Apply(AudienceSegment segment, AudienceSegmentRequest request)
    {
        string? rules = null;
        if (request.TargetingRules is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } json)
        {
            if (json.ValueKind != JsonValueKind.Object)
                throw new BadRequestException("targetingRules must be a JSON object.");
            rules = json.GetRawText();
            if (rules.Length > MaxRulesLength)
                throw new BadRequestException($"targetingRules is too large (max {MaxRulesLength} characters).");
        }

        segment.Name = request.Name!.Trim();
        segment.Description = request.Description?.Trim();
        segment.TargetingRulesJson = rules;
    }

    private static AudienceSegmentResponse ToResponse(AudienceSegment a) => new(
        a.Id,
        a.BusinessId,
        a.Name,
        a.Description,
        a.TargetingRulesJson is null ? null : JsonDocument.Parse(a.TargetingRulesJson).RootElement.Clone(),
        a.CreatedAt);
}
