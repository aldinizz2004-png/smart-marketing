using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.DTOs.Offers;

public sealed record OfferResponse(
    Guid Id,
    Guid BusinessId,
    string Title,
    string? Description,
    decimal? OfferPrice,
    string? Currency,
    decimal? DiscountPercent,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    StatusInfo Status,
    bool IsRunningNow,                                          // Active and inside its dates
    DateTimeOffset CreatedAt);
