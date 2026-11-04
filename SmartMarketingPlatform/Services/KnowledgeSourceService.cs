using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Common.Storage;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.DTOs.Knowledge;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Pasted text and plain-text files are usable at once (Processed).
// URLs and PDFs wait as Pending until the background worker extracts their text (later phase).
public sealed class KnowledgeSourceService(
    MarketingDbContext db,
    IBusinessAccess access,
    IFileStorage storage,
    StatusCatalog statuses,
    TimeProvider clock) : IKnowledgeSourceService
{
    private const int PreviewLength = 200;
    private const int MaxTextLength = 50_000;

    // Processed and Failed are set by the system; the client can only disable or re-queue.
    private static readonly int[] ClientTargets = [StatusIds.KnowledgeSource.Disabled, StatusIds.KnowledgeSource.Pending];

    private static readonly StatusFlow Flow = new("knowledge source", new Dictionary<int, int[]>
    {
        [StatusIds.KnowledgeSource.Pending] = [StatusIds.KnowledgeSource.Processed, StatusIds.KnowledgeSource.Failed, StatusIds.KnowledgeSource.Disabled],
        [StatusIds.KnowledgeSource.Processed] = [StatusIds.KnowledgeSource.Disabled],
        [StatusIds.KnowledgeSource.Failed] = [StatusIds.KnowledgeSource.Pending, StatusIds.KnowledgeSource.Disabled],
        [StatusIds.KnowledgeSource.Disabled] = [StatusIds.KnowledgeSource.Pending]
    });

    private sealed record Row(Guid Id, Guid BusinessId, KnowledgeSourceType SourceType, string? SourceUrl, Guid? AssetId,
        int StatusId, int TextLength, string? Preview, DateTimeOffset? ProcessedAt, DateTimeOffset CreatedAt);

    public async Task<PagedResult<KnowledgeSourceResponse>> GetAllAsync(Guid businessId, KnowledgeListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var sources = db.KnowledgeSources.AsNoTracking().Where(k => k.BusinessId == businessId);
        if (query.Type is not null) sources = sources.Where(k => k.SourceType == query.Type);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusId = statuses.GetId("KnowledgeSource", query.Status);
            sources = sources.Where(k => k.StatusId == statusId);
        }

        // Only the length and a preview of the text leave SQL Server, not the whole text.
        var page = await sources
            .OrderByDescending(k => k.CreatedAt).ThenByDescending(k => k.Id)
            .Select(k => new Row(k.Id, k.BusinessId, k.SourceType, k.SourceUrl, k.AssetId, k.StatusId,
                k.ExtractedText == null ? 0 : k.ExtractedText.Length,
                k.ExtractedText == null ? null : k.ExtractedText.Substring(0, PreviewLength),
                k.ProcessedAt, k.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);

        return page.Map(r => new KnowledgeSourceResponse(r.Id, r.BusinessId, r.SourceType, r.SourceUrl, r.AssetId,
            statuses.Describe(r.StatusId), r.TextLength, r.Preview, null, r.ProcessedAt, r.CreatedAt));
    }

    public async Task<KnowledgeSourceResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken), includeText: true);
    }

    public async Task<KnowledgeSourceResponse> CreateAsync(Guid businessId, KnowledgeSourceRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var hasUrl = !string.IsNullOrWhiteSpace(request.SourceUrl);
        var hasText = !string.IsNullOrWhiteSpace(request.Text);
        var hasAsset = request.AssetId is not null;
        if ((hasUrl ? 1 : 0) + (hasText ? 1 : 0) + (hasAsset ? 1 : 0) != 1)
            throw new BadRequestException("Send exactly one of: sourceUrl, assetId or text.");

        var now = clock.GetUtcNow();
        var source = new KnowledgeSource
        {
            BusinessId = businessId,
            SourceType = request.SourceType!.Value,
            StatusId = StatusIds.KnowledgeSource.Pending
        };

        if (hasUrl)
        {
            if (!Uri.TryCreate(request.SourceUrl!.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new BadRequestException("sourceUrl must be an absolute http(s) URL.");
            source.SourceUrl = uri.ToString();
        }
        else if (hasText)
        {
            MarkProcessed(source, request.Text!.Trim(), now);
        }
        else
        {
            var asset = await db.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.AssetId && m.BusinessId == businessId, cancellationToken)
                ?? throw new NotFoundException("Media asset not found.");
            if (asset.Kind != MediaKind.Document)
                throw new BadRequestException("A knowledge source file must be a Document asset (PDF or text).");

            source.AssetId = asset.Id;
            if (asset.MimeType == "text/plain")
            {
                await using var stream = await storage.OpenReadAsync(asset.ObjectKey, cancellationToken);
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync(cancellationToken);
                MarkProcessed(source, text.Length > MaxTextLength ? text[..MaxTextLength] : text, now);
            }
        }

        db.KnowledgeSources.Add(source);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(source, includeText: true);
    }

    public async Task<KnowledgeSourceResponse> ChangeStatusAsync(Guid businessId, Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var source = await LoadAsync(businessId, id, cancellationToken);

        var target = statuses.GetId("KnowledgeSource", request.Status!);
        if (!ClientTargets.Contains(target))
            throw new BadRequestException("Only 'Disabled' or 'Pending' can be requested; Processed and Failed are set by the system.");

        Flow.EnsureCanMove(source.StatusId, target, statuses);

        // Re-enabling pasted text needs no processing: it is ready again at once.
        var isPastedText = source.SourceUrl is null && source.AssetId is null && source.ExtractedText is not null;
        source.StatusId = target == StatusIds.KnowledgeSource.Pending && isPastedText
            ? StatusIds.KnowledgeSource.Processed
            : target;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(source, includeText: false);
    }

    public async Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var source = await LoadAsync(businessId, id, cancellationToken);
        db.KnowledgeSources.Remove(source);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void MarkProcessed(KnowledgeSource source, string text, DateTimeOffset now)
    {
        source.ExtractedText = text;
        source.StatusId = StatusIds.KnowledgeSource.Processed;
        source.ProcessedAt = now;
    }

    private async Task<KnowledgeSource> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.KnowledgeSources.FirstOrDefaultAsync(k => k.Id == id && k.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Knowledge source not found.");

    private KnowledgeSourceResponse ToResponse(KnowledgeSource k, bool includeText) => new(
        k.Id, k.BusinessId, k.SourceType, k.SourceUrl, k.AssetId, statuses.Describe(k.StatusId),
        k.ExtractedText?.Length ?? 0,
        k.ExtractedText is null ? null : k.ExtractedText[..Math.Min(PreviewLength, k.ExtractedText.Length)],
        includeText ? k.ExtractedText : null,
        k.ProcessedAt, k.CreatedAt);
}
