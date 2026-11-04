using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.DTOs.Account;

public sealed record MeResponse(
    Guid Id,
    string Email,
    string? FullName,
    string PreferredLanguage,
    DateTimeOffset CreatedAt,
    Guid CurrentTenantId,
    IReadOnlyList<MembershipResponse> Memberships);

public sealed record MembershipResponse(
    Guid TenantId,
    string TenantName,
    MembershipRole Role,
    StatusInfo Status,
    DateTimeOffset JoinedAt);
