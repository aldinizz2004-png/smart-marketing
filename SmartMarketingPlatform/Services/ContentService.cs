using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Brand;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Content;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Content life cycle: Draft -> InReview -> Approved (-> Scheduled -> Published in the publishing phase).
// Every change of text is a NEW revision; a review always points to the exact revision it judged.
public sealed class ContentService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    StatusCatalog statuses,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    IBrandGuard brandGuard,
    IPublishingService publishing,
    TimeProvider clock) : IContentService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly StatusFlow Flow = new("content item", new Dictionary<int, int[]>
    {
        [StatusIds.ContentItem.Draft] = [StatusIds.ContentItem.InReview, StatusIds.ContentItem.Canceled],
        [StatusIds.ContentItem.InReview] = [StatusIds.ContentItem.Draft, StatusIds.ContentItem.Approved, StatusIds.ContentItem.Canceled],
        [StatusIds.ContentItem.Approved] = [StatusIds.ContentItem.Draft, StatusIds.ContentItem.Scheduled, StatusIds.ContentItem.Canceled],
        [StatusIds.ContentItem.Scheduled] = [StatusIds.ContentItem.Approved, StatusIds.ContentItem.Published, StatusIds.ContentItem.Canceled],
        [StatusIds.ContentItem.Published] = [],
        [StatusIds.ContentItem.Canceled] = []
    });

    // Approved comes from a review; Scheduled/Published from the publishing phase.
    private static readonly int[] ClientTargets =
        [StatusIds.ContentItem.InReview, StatusIds.ContentItem.Draft, StatusIds.ContentItem.Canceled];

    private static readonly int[] ClosedCampaign = [StatusIds.Campaign.Completed, StatusIds.Campaign.Canceled];

    // How many media files each format takes, and of which kind (null prefix = image or video).
    private static readonly Dictionary<ContentFormat, (int Min, int Max, string? MimePrefix)> MediaRules = new()
    {
        [ContentFormat.Text] = (0, 0, null),
        [ContentFormat.SingleImage] = (1, 1, "image/"),
        [ContentFormat.Carousel] = (2, 10, "image/"),
        [ContentFormat.Video] = (1, 1, "video/"),
        [ContentFormat.Reel] = (1, 1, "video/"),
        [ContentFormat.Story] = (1, 1, null),
        [ContentFormat.Live] = (0, 0, null)
    };

    // ---- queries ----------------------------------------------------------------

    public async Task<PagedResult<ContentItemResponse>> GetAllAsync(Guid businessId, ContentListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var items = db.ContentItems.AsNoTracking().Where(i => i.Campaign.BusinessId == businessId);
        if (query.CampaignId is not null) items = items.Where(i => i.CampaignId == query.CampaignId);
        if (!string.IsNullOrWhiteSpace(query.Platform))
        {
            var platform = query.Platform.Trim().ToLowerInvariant();
            items = items.Where(i => i.PlatformCode == platform);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("ContentItem", query.Status);
            items = items.Where(i => i.StatusId == statusId);
        }

        // Only the latest revision of each item is loaded for a list.
        var page = await items
            .Include(i => i.Revisions.OrderByDescending(r => r.RevisionNo).Take(1)).ThenInclude(r => r.Assets).ThenInclude(a => a.Asset)
            .Include(i => i.Revisions.OrderByDescending(r => r.RevisionNo).Take(1)).ThenInclude(r => r.Reviews)
            .AsSplitQuery()
            .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            .ToPagedResultAsync(query, cancellationToken);

        return page.Map(i => ToResponse(i, businessId, includeAll: false));
    }

    public async Task<ContentItemResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var item = await db.ContentItems.AsNoTracking()
                       .Include(i => i.Revisions).ThenInclude(r => r.Assets).ThenInclude(a => a.Asset)
                       .Include(i => i.Revisions).ThenInclude(r => r.Reviews)
                       .AsSplitQuery()
                       .FirstOrDefaultAsync(i => i.Id == id && i.Campaign.BusinessId == businessId, cancellationToken)
                   ?? throw new NotFoundException("Content item not found.");
        return ToResponse(item, businessId, includeAll: true);
    }

    // ---- commands ---------------------------------------------------------------

    public async Task<ContentItemResponse> CreateAsync(Guid businessId, CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var campaign = await LoadOpenCampaignAsync(businessId, request.CampaignId!.Value, cancellationToken);
        var platform = await ResolvePlatformAsync(request.PlatformCode!, cancellationToken);
        var type = request.ContentType!.Value;

        // The revision (and its AI call) is prepared BEFORE the item is tracked,
        // so a failure never leaves an item without text in the database.
        var revision = await PrepareRevisionAsync(businessId, campaign, platform, type, request.FirstRevision!, null, null, cancellationToken);
        revision.RevisionNo = 1;

        var item = new ContentItem
        {
            CampaignId = campaign.Id,
            PlatformCode = platform,
            ContentType = type,
            StatusId = StatusIds.ContentItem.Draft,
            PlannedPublishAt = request.PlannedPublishAt,
            VariantLabel = request.VariantLabel?.Trim()
        };
        item.Revisions.Add(revision);
        db.ContentItems.Add(item);

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(businessId, item.Id, cancellationToken);
    }

    public async Task<ContentItemResponse> AddRevisionAsync(Guid businessId, Guid id, RevisionRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var item = await LoadItemAsync(businessId, id, cancellationToken);

        if (item.StatusId == StatusIds.ContentItem.InReview)
            throw new ConflictException("This content is under review. Withdraw it first (PATCH status to Draft).");
        if (item.StatusId is not (StatusIds.ContentItem.Draft or StatusIds.ContentItem.Approved))
            throw new ConflictException($"{statuses.CodeOf(item.StatusId)} content cannot get new revisions.");

        var latest = await db.ContentRevisions.AsNoTracking()
            .Where(r => r.ContentItemId == id)
            .OrderByDescending(r => r.RevisionNo)
            .FirstAsync(cancellationToken);

        var revision = await PrepareRevisionAsync(businessId, item.Campaign, item.PlatformCode, item.ContentType, request, latest.Body, null, cancellationToken);
        revision.ContentItemId = id;
        revision.RevisionNo = latest.RevisionNo + 1;     // unique (ContentItemId, RevisionNo): a parallel request gets 409
        db.ContentRevisions.Add(revision);

        // An approval was for the old text: new text needs a new review.
        item.StatusId = StatusIds.ContentItem.Draft;

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(businessId, id, cancellationToken);
    }

    public async Task<ContentItemResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var item = await LoadItemAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("ContentItem", request.Status!);
        if (!ClientTargets.Contains(target))
            throw new BadRequestException("Approved comes from a review (POST .../reviews); Scheduled and Published from publishing.");
        Flow.EnsureCanMove(item.StatusId, target, statuses);

        // Drafts may be incomplete; content sent to review must have its media.
        if (target == StatusIds.ContentItem.InReview && item.StatusId != target)
        {
            var latest = await db.ContentRevisions.AsNoTracking()
                .Where(r => r.ContentItemId == id)
                .OrderByDescending(r => r.RevisionNo)
                .Select(r => new { r.Format, Media = r.Assets.Count })
                .FirstAsync(cancellationToken);
            var min = MediaRules[latest.Format].Min;
            if (latest.Media < min)
                throw new ConflictException($"A {latest.Format} needs at least {min} media file(s) before it can be reviewed.");
        }

        // Canceling scheduled content also cancels its waiting publication job.
        if (target == StatusIds.ContentItem.Canceled && item.StatusId == StatusIds.ContentItem.Scheduled)
            await publishing.CancelActiveJobsAsync(id, cancellationToken);

        item.StatusId = target;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(businessId, id, cancellationToken);
    }

    public async Task<ContentItemResponse> ReviewAsync(Guid businessId, Guid id, ReviewRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var item = await LoadItemAsync(businessId, id, cancellationToken);
        if (item.StatusId != StatusIds.ContentItem.InReview)
            throw new ConflictException("Only content that is in review can be reviewed.");

        var decision = statuses.GetId("ContentReview", request.Decision!);
        var feedback = request.Feedback?.Trim();
        if (decision != StatusIds.ContentReview.Approved && string.IsNullOrEmpty(feedback))
            throw new BadRequestException("Feedback is required when changes are requested or the content is rejected.");

        var latest = await db.ContentRevisions
            .Where(r => r.ContentItemId == id)
            .OrderByDescending(r => r.RevisionNo)
            .FirstAsync(cancellationToken);

        // The owner's rule: the author of a text cannot approve it.
        if (decision == StatusIds.ContentReview.Approved && latest.CreatedBy == currentUser.UserId)
        {
            var separate = await db.AutomationPolicies
                .AnyAsync(p => p.BusinessId == businessId && p.RequireSeparateReviewer, cancellationToken);
            if (separate)
                throw new ForbiddenException("The owner requires a different person to approve content you wrote.");
        }

        db.ContentReviews.Add(new ContentReview
        {
            RevisionId = latest.Id,
            ReviewerId = currentUser.UserId,
            StatusId = decision,
            Feedback = feedback,
            DecidedAt = clock.GetUtcNow()
        });

        item.StatusId = decision switch
        {
            StatusIds.ContentReview.Approved => StatusIds.ContentItem.Approved,
            StatusIds.ContentReview.ChangesRequested => StatusIds.ContentItem.Draft,
            _ => StatusIds.ContentItem.Canceled                                     // Rejected
        };

        await db.SaveChangesAsync(cancellationToken);

        // If the owner allows automation on this platform, approved content is scheduled at once.
        if (decision == StatusIds.ContentReview.Approved)
            await publishing.TryAutoScheduleAsync(businessId, id, cancellationToken);

        return await GetAsync(businessId, id, cancellationToken);
    }

    // An accepted NewContent recommendation becomes a content item written by the AI.
    public async Task<ContentItemResponse> ApplyRecommendationAsync(Guid businessId, Guid recommendationId, ApplyRecommendationRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var recommendation = await db.StrategyRecommendations
                                 .FirstOrDefaultAsync(r => r.Id == recommendationId && r.StrategyRun.BusinessId == businessId, cancellationToken)
                             ?? throw new NotFoundException("Recommendation not found.");

        if (recommendation.StatusId == StatusIds.Recommendation.Applied)
            throw new ConflictException("This recommendation was already applied.");
        if (recommendation.StatusId != StatusIds.Recommendation.Accepted)
            throw new ConflictException("Accept the recommendation before applying it.");
        if (recommendation.ActionType != RecommendationAction.NewContent)
            throw new ConflictException("Only NewContent recommendations create content items.");

        var campaignId = recommendation.CampaignId ?? request.CampaignId
                         ?? throw new BadRequestException("This recommendation is not tied to a campaign: send campaignId.");
        var campaign = await LoadOpenCampaignAsync(businessId, campaignId, cancellationToken);

        var payload = JsonNode.Parse(recommendation.ProposedPayloadJson)!.AsObject();
        var platform = await ResolvePlatformAsync(Text(payload, "platform") ?? "instagram", cancellationToken);
        var type = Enum.TryParse<ContentType>(Text(payload, "contentType"), out var t) ? t : ContentType.Post;

        var revisionRequest = new RevisionRequest
        {
            GenerateWithAi = true,
            Format = Enum.TryParse<ContentFormat>(Text(payload, "format"), out var f) ? f : null,
            HookType = Enum.TryParse<HookType>(Text(payload, "hook"), out var h) ? h : null,
            ProductId = Id(payload, "productId"),
            OfferId = Id(payload, "offerId"),
            Instructions = Text(payload, "captionIdea")
        };
        var revision = await PrepareRevisionAsync(businessId, campaign, platform, type, revisionRequest, null, recommendation.Id, cancellationToken);
        revision.RevisionNo = 1;

        var item = new ContentItem
        {
            CampaignId = campaign.Id,
            PlatformCode = platform,
            ContentType = type,
            StatusId = StatusIds.ContentItem.Draft,
            PlannedPublishAt = recommendation.ProposedPublishAt,
            SourceRecommendationId = recommendation.Id
        };
        item.Revisions.Add(revision);
        db.ContentItems.Add(item);

        recommendation.StatusId = StatusIds.Recommendation.Applied;
        recommendation.AppliedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(businessId, item.Id, cancellationToken);
    }

    // ---- building a revision ----------------------------------------------------

    private async Task<ContentRevision> PrepareRevisionAsync(
        Guid businessId, Campaign campaign, string platform, ContentType type,
        RevisionRequest request, string? previousBody, Guid? sourceRecommendationId, CancellationToken cancellationToken)
    {
        ContentRevision revision;

        if (request.GenerateWithAi)
        {
            if (!string.IsNullOrWhiteSpace(request.Text))
                throw new BadRequestException("Send either text (written by you) or generateWithAi, not both.");

            var product = request.ProductId is { } productId
                ? await db.Products.AsNoTracking().Where(p => p.Id == productId && p.BusinessId == businessId)
                      .Select(p => new ProductInfo(p.Id, p.Name, p.Price, p.Currency)).FirstOrDefaultAsync(cancellationToken)
                  ?? throw new BadRequestException("Unknown productId for this business.")
                : null;
            var offer = request.OfferId is { } offerId
                ? await db.Offers.AsNoTracking().Where(o => o.Id == offerId && o.BusinessId == businessId && o.StatusId != StatusIds.Offer.Ended)
                      .Select(o => new OfferInfo(o.Id, o.Title, o.DiscountPercent, o.OfferPrice, o.EndsAt)).FirstOrDefaultAsync(cancellationToken)
                  ?? throw new BadRequestException("Unknown or ended offerId for this business.")
                : null;

            var context = await contextBuilder.BuildAsync(businessId, campaign.Id, cancellationToken);
            var brief = new ContentBrief(platform, type, request.Format, request.HookType, product, offer, request.Instructions, previousBody);

            var (draft, execution) = await aiRunner.RunAsync(
                businessId, AiTaskType.Copy, UsageEventType.CopyGeneration, new { brief, context },
                ct => ai.WriteContentAsync(context, brief, ct), cancellationToken);

            // The AI is checked like a person: banned words are never saved.
            var banned = await brandGuard.FindProhibitedTermsAsync(businessId, draft.Body, cancellationToken);
            if (banned.Count > 0)
                throw new BadGatewayException($"The AI text used prohibited words ({string.Join(", ", banned)}) and was discarded. Execution {execution.Id}.");

            revision = new ContentRevision
            {
                Body = draft.Body,
                Format = draft.Format,
                HookType = draft.HookType,
                CallToAction = draft.CallToAction,
                IncludesOffer = draft.IncludesOffer,
                CreativeDetailsJson = draft.CreativeDetails.ToJsonString(),
                // What the AI relied on (prices, banned words), to explain the text later.
                GenerationContextJson = JsonSerializer.Serialize(new
                {
                    brief,
                    prohibitedTerms = context.ProhibitedTerms,
                    prices = context.Products.Select(p => new { p.Name, p.Price, p.Currency })
                }, Json),
                GeneratedByExecutionId = execution.Id,
                CreatedBy = null
            };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new BadRequestException("text is required (or set generateWithAi to true).");
            if (request.Format is null)
                throw new BadRequestException("format is required for a text you write.");
            if (request.ProductId is not null || request.OfferId is not null || request.Instructions is not null)
                throw new BadRequestException("productId, offerId and instructions are only used with generateWithAi.");

            var text = request.Text.Trim();
            var banned = await brandGuard.FindProhibitedTermsAsync(businessId, text, cancellationToken);
            if (banned.Count > 0)
                throw new BadRequestException($"The text uses prohibited brand words: {string.Join(", ", banned)}.");

            revision = new ContentRevision
            {
                Body = text,
                Format = request.Format.Value,
                HookType = request.HookType,
                CallToAction = request.CallToAction,
                IncludesOffer = request.IncludesOffer ?? false,
                CreatedBy = currentUser.UserId
            };
        }

        revision.SourceRecommendationId = sourceRecommendationId;
        await AttachAssetsAsync(businessId, revision, request.AssetIds, cancellationToken);
        return revision;
    }

    private async Task AttachAssetsAsync(Guid businessId, ContentRevision revision, List<Guid> assetIds, CancellationToken cancellationToken)
    {
        var ids = assetIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var assets = await db.MediaAssets.AsNoTracking()
            .Where(m => m.BusinessId == businessId && ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        if (assets.Count != ids.Count)
            throw new BadRequestException("One or more assetIds are unknown for this business.");

        var (_, max, prefix) = MediaRules[revision.Format];
        if (ids.Count > max)
            throw new BadRequestException($"A {revision.Format} takes at most {max} media file(s).");

        foreach (var asset in assets.Values)
        {
            var visual = asset.MimeType.StartsWith("image/", StringComparison.Ordinal) || asset.MimeType.StartsWith("video/", StringComparison.Ordinal);
            if (!visual || (prefix is not null && !asset.MimeType.StartsWith(prefix, StringComparison.Ordinal)))
                throw new BadRequestException($"A {revision.Format} cannot use a {asset.MimeType} file.");
        }

        for (var i = 0; i < ids.Count; i++)
            revision.Assets.Add(new ContentRevisionAsset { AssetId = ids[i], Position = (byte)(i + 1) });
    }

    // ---- helpers ----------------------------------------------------------------

    private async Task<ContentItem> LoadItemAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.ContentItems.Include(i => i.Campaign)
            .FirstOrDefaultAsync(i => i.Id == id && i.Campaign.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Content item not found.");

    private async Task<Campaign> LoadOpenCampaignAsync(Guid businessId, Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken)
                       ?? throw new NotFoundException("Campaign not found.");
        if (ClosedCampaign.Contains(campaign.StatusId))
            throw new ConflictException("The campaign is completed or canceled.");
        return campaign;
    }

    private async Task<string> ResolvePlatformAsync(string code, CancellationToken cancellationToken)
    {
        var value = code.Trim().ToLowerInvariant();
        if (!await db.Platforms.AnyAsync(p => p.Code == value && p.IsActive && p.SupportsPublishing, cancellationToken))
            throw new BadRequestException($"'{value}' is not a publishing platform. See /api/platforms.");
        return value;
    }

    private static string? Text(JsonObject payload, string name) =>
        payload[name] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static Guid? Id(JsonObject payload, string name) =>
        Guid.TryParse(Text(payload, name), out var id) ? id : null;

    private ContentItemResponse ToResponse(ContentItem i, Guid businessId, bool includeAll)
    {
        var revisions = i.Revisions.OrderByDescending(r => r.RevisionNo).Select(r => ToRevisionResponse(r, businessId)).ToList();
        return new ContentItemResponse(
            i.Id, businessId, i.CampaignId, i.PlatformCode, i.ContentType, statuses.Describe(i.StatusId),
            i.PlannedPublishAt, i.VariantLabel, i.SourceRecommendationId,
            revisions.FirstOrDefault(),
            includeAll ? revisions : null,
            i.CreatedAt);
    }

    private ContentRevisionResponse ToRevisionResponse(ContentRevision r, Guid businessId) => new(
        r.Id, r.RevisionNo, r.Body, r.Format, r.HookType, r.CallToAction, r.IncludesOffer,
        r.CreativeDetailsJson is null ? null : JsonDocument.Parse(r.CreativeDetailsJson).RootElement.Clone(),
        r.GeneratedByExecutionId is null ? "user" : "ai",
        r.CreatedBy, r.GeneratedByExecutionId,
        r.Assets.OrderBy(a => a.Position)
            .Select(a => new RevisionAssetResponse(a.AssetId, a.Position, a.Asset.MimeType, $"/api/businesses/{businessId}/media/{a.AssetId}/content"))
            .ToList(),
        r.Reviews.OrderBy(v => v.DecidedAt)
            .Select(v => new ContentReviewResponse(v.Id, v.ReviewerId, statuses.Describe(v.StatusId), v.Feedback, v.DecidedAt))
            .ToList(),
        r.CreatedAt);
}
