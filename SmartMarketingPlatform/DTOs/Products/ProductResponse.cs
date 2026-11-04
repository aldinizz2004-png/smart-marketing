namespace SmartMarketingPlatform.DTOs.Products;

public sealed record ProductResponse(
    Guid Id,
    Guid BusinessId,
    string? Sku,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
