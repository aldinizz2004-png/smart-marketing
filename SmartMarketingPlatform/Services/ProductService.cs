using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Businesses;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Products;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public sealed class ProductService(MarketingDbContext db, IBusinessAccess access) : IProductService
{
    public async Task<PagedResult<ProductResponse>> GetAllAsync(Guid businessId, ProductListQuery query, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);

        var products = db.Products.AsNoTracking().Where(p => p.BusinessId == businessId);
        if (query.IsActive is not null)
            products = products.Where(p => p.IsActive == query.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));
        }

        var page = await products
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .ToPagedResultAsync(query, cancellationToken);
        return page.Map(ToResponse);
    }

    public async Task<ProductResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetAsync(businessId, cancellationToken);
        return ToResponse(await LoadAsync(businessId, id, cancellationToken));
    }

    public async Task<ProductResponse> CreateAsync(Guid businessId, ProductRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        await EnsureSkuIsFreeAsync(businessId, request.Sku, null, cancellationToken);

        var product = new Product { BusinessId = businessId };
        Apply(product, request, business.DefaultCurrency);

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid businessId, Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var business = await access.GetForWriteAsync(businessId, cancellationToken);
        var product = await LoadAsync(businessId, id, cancellationToken);
        await EnsureSkuIsFreeAsync(businessId, request.Sku, id, cancellationToken);

        Apply(product, request, business.DefaultCurrency);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    // A product already used by a campaign or a photo is part of history: deactivate it instead.
    public async Task DeleteAsync(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await access.GetForWriteAsync(businessId, cancellationToken);
        var product = await LoadAsync(businessId, id, cancellationToken);

        var inUse = await db.CampaignProducts.AnyAsync(cp => cp.ProductId == id, cancellationToken)
                    || await db.MediaAssets.AnyAsync(m => m.ProductId == id, cancellationToken);
        if (inUse)
            throw new ConflictException("This product is used by a campaign or media; set isActive=false instead of deleting it.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSkuIsFreeAsync(Guid businessId, string? sku, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku)) return;
        var value = sku.Trim();
        if (await db.Products.AnyAsync(p => p.BusinessId == businessId && p.Sku == value && p.Id != exceptId, cancellationToken))
            throw new ConflictException($"Another product of this business already has SKU '{value}'.");
    }

    private async Task<Product> LoadAsync(Guid businessId, Guid id, CancellationToken cancellationToken) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId, cancellationToken)
        ?? throw new NotFoundException("Product not found.");

    private static void Apply(Product product, ProductRequest request, string defaultCurrency)
    {
        product.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        product.Name = request.Name!.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price!.Value;
        product.Currency = request.Currency ?? defaultCurrency;
        product.IsActive = request.IsActive ?? true;
    }

    private static ProductResponse ToResponse(Product p) => new(
        p.Id, p.BusinessId, p.Sku, p.Name, p.Description, p.Price, p.Currency, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
