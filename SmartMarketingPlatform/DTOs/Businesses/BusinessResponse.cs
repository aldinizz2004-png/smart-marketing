using SmartMarketingPlatform.DTOs.Common;

namespace SmartMarketingPlatform.DTOs.Businesses;

public sealed record BusinessResponse(
    Guid Id,
    string Name,
    string Category,
    string? CountryCode,
    string? City,
    string? Address,
    string TimeZoneId,
    string DefaultCurrency,
    string? ContactPhone,
    string? Description,
    StatusInfo Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
