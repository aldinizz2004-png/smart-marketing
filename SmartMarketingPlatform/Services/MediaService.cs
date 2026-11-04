using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Common.Storage;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Media;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public sealed class MediaService(
    MarketingDbContext db,
    IBusinessAccess access,
    ICurrentUser currentUser,
    IFileStorage storage) : IMediaService
{
    public const long MaxUploadBytes = 50L * 1024 * 1024;   // the largest kind (video)
    private const long Mb = 1024 * 1024;

    private static readonly string[] Images = ["image/jpeg", "image/png", "image/webp"];

    // What each kind of asset may be, decided from the file's real bytes.
    private static readonly Dictionary<MediaKind, (string[] MimeTypes, long MaxBytes)> Rules = new()
    {
        [MediaKind.Logo] = (Images, 5 * Mb),
        [MediaKind.ProductPhoto] = (Images, 10 * Mb),
        [MediaKind.BrandImage] = (Images, 10 * Mb),
        [MediaKind.GeneratedImage] = (Images, 10 * Mb),
        [MediaKind.Video] = (["video/mp4"], MaxUploadBytes),
        [MediaKind.Document] = (["application/pdf", "text/plain"], 10 * Mb)
    };

    public async Task<PagedResult<MediaAssetResponse>> GetAllAsync(Guid businessId, MediaListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var assets = db.MediaAssets.AsNoTracking().Where(m => m.BusinessId == businessId);
        if (query.Kind is not null) assets = assets.Where(m => m.Kind == query.Kind);
        if (query.ProductId is not null) assets = assets.Where(m => m.ProductId == query.ProductId);

        var page = await assets
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<MediaAssetResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken));
    }

    public async Task<MediaAssetResponse> UploadAsync(Guid businessId, UploadMediaRequest request, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);

        var file = request.File!;
        var kind = request.Kind!.Value;
        var (allowedTypes, maxBytes) = Rules[kind];

        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");
        if (file.Length > maxBytes)
            throw new PayloadTooLargeException($"A {kind} can be at most {maxBytes / Mb} MB.");
        if (request.ProductId is { } productId
            && !await db.Products.AnyAsync(p => p.Id == productId && p.BusinessId == businessId, cancellationToken))
            throw new NotFoundException("Product not found.");

        await using var input = file.OpenReadStream();

        // 1) Real type from the first bytes (never the file name or the client's Content-Type).
        var header = new byte[FileSignature.HeaderLength];
        var read = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var detected = FileSignature.Detect(header.AsSpan(0, read))
                       ?? throw new UnsupportedMediaTypeException("This file type is not supported.");
        if (!allowedTypes.Contains(detected.MimeType))
            throw new UnsupportedMediaTypeException(
                $"A {kind} must be {string.Join(" or ", allowedTypes)}; this file is {detected.MimeType}.");

        // 2) Fingerprint: the same file is stored once per business.
        input.Seek(0, SeekOrigin.Begin);
        var sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(input, cancellationToken));
        var existingId = await db.MediaAssets
            .Where(m => m.BusinessId == businessId && m.Sha256 == sha256)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingId is not null)
            throw new ConflictException($"This file was already uploaded (asset {existingId}).");

        // 3) Store the bytes, then the row. If the row fails, remove the file (no orphans).
        var asset = new MediaAsset
        {
            BusinessId = businessId,
            ProductId = request.ProductId,
            Kind = kind,
            MimeType = detected.MimeType,
            ByteSize = file.Length,
            Sha256 = sha256
        };
        db.MediaAssets.Add(asset);   // EF assigns the Guid here
        asset.ObjectKey = $"{currentUser.TenantId:N}/{businessId:N}/{asset.Id:N}{detected.Extension}";

        input.Seek(0, SeekOrigin.Begin);
        await storage.SaveAsync(asset.ObjectKey, input, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(asset.ObjectKey, CancellationToken.None);
            throw;
        }

        return ToResponse(asset);
    }

    public async Task<MediaContent> OpenAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        var asset = await LoadAsync(businessId, id, cancellationToken);
        var stream = await storage.OpenReadAsync(asset.ObjectKey, cancellationToken);
        return new MediaContent(stream, asset.MimeType, asset.Sha256, asset.CreatedAt);
    }

    public async Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var asset = await LoadAsync(businessId, id, cancellationToken);

        var inUse = await db.KnowledgeSources.AnyAsync(k => k.AssetId == id, cancellationToken)
                    || await db.ContentRevisionAssets.AnyAsync(r => r.AssetId == id, cancellationToken);
        if (inUse)
            throw new ConflictException("This file is used by a knowledge source or by content and cannot be deleted.");

        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(asset.ObjectKey, cancellationToken);   // row first: a leftover file is harmless, a dangling row is not
    }

    private async Task<MediaAsset> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id && m.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Media asset not found.");

    private static MediaAssetResponse ToResponse(MediaAsset m) => new(
        m.Id, m.BusinessId, m.ProductId, m.Kind, m.MimeType, m.ByteSize, m.Sha256,
        $"/api/businesses/{m.BusinessId}/media/{m.Id}/content",
        m.CreatedAt);
}
