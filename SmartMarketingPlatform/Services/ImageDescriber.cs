using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Storage;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// The AI looks at a photo once. Its description is stored on the asset and reused as text, so later
// posts, plans and WhatsApp answers "know" the product's look without sending the picture again
// (and the text-only models of the chain can use it too).
public sealed class ImageDescriber(
    MarketingDbContext db,
    IFileStorage storage,
    IMarketingAi ai,
    IAiExecutionRunner aiRunner,
    IMarketingContextBuilder contextBuilder,
    TimeProvider clock) : IImageDescriber
{
    private const long MaxImageBytes = 10L * 1024 * 1024;

    public async Task DescribeAsync(MediaAsset asset, bool force, CancellationToken cancellationToken)
    {
        if (!asset.MimeType.StartsWith("image/", StringComparison.Ordinal))
            throw new BadRequestException("Only images can be described.");
        if (asset.DescribedAt is not null && !force)
            return;
        if (asset.ByteSize > MaxImageBytes)
            throw new BadRequestException("The image is too large for the AI (10 MB at most).");

        byte[] data;
        await using (var stream = await storage.OpenReadAsync(asset.ObjectKey, cancellationToken))
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken);
            data = buffer.ToArray();
        }

        // Files uploaded before sizes were recorded get them now.
        if (asset.Width is null && ImageSize.Read(data) is { } size)
            (asset.Width, asset.Height) = size;

        var context = await contextBuilder.BuildAsync(asset.BusinessId, null, cancellationToken);
        var product = asset.ProductId is { } productId
            ? context.Products.FirstOrDefault(p => p.Id == productId)
              ?? await db.Products.AsNoTracking().Where(p => p.Id == productId)
                  .Select(p => new ProductInfo(p.Id, p.Name, p.Price, p.Currency)).FirstOrDefaultAsync(cancellationToken)
            : null;

        var image = new ImageInput(asset.MimeType, data, asset.Width, asset.Height);
        var (draft, execution) = await aiRunner.RunAsync(
            asset.BusinessId, AiTaskType.ImageDescription, UsageEventType.ImageDescription,
            new { assetId = asset.Id, asset.MimeType, asset.Width, asset.Height, product = product?.Name },
            ct => ai.DescribeImageAsync(context, image, product, ct), cancellationToken);

        asset.VisualDescription = draft.Description.Length <= 1000 ? draft.Description : draft.Description[..1000];
        asset.ShowsProduct = draft.ShowsProduct;
        asset.QualityIssuesJson = JsonSerializer.Serialize(draft.Issues.Select(i => i.ToString()));
        asset.QualityAdvice = draft.Advice is { Length: > 500 } advice ? advice[..500] : draft.Advice;
        asset.DescribedAt = clock.GetUtcNow();
        asset.DescribedByExecutionId = execution.Id;
        await db.SaveChangesAsync(cancellationToken);
    }
}
