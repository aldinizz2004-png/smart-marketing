using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Brand;

public sealed record BrandTermResponse(Guid Id, string Term, BrandTermRule Rule, string? Note);
