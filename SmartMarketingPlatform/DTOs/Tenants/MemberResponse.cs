using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Tenants;

public sealed record MemberResponse(
    Guid UserId,
    string Email,
    string? FullName,
    MembershipRole Role,
    StatusInfo Status,
    DateTimeOffset JoinedAt);
