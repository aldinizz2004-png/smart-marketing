using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// Everything the AI is told about a business. It is also saved as the input snapshot
// of the AiExecution, so every answer can be explained later.
public sealed record MarketingContext(
    Guid BusinessId,
    string BusinessName,
    string Category,
    string? City,
    string TimeZoneId,
    string Currency,
    string? Dialect,
    string? Tone,
    byte EmojiLevel,
    IReadOnlyList<string> PreferredTerms,
    IReadOnlyList<string> ProhibitedTerms,
    IReadOnlyList<ProductInfo> Products,
    IReadOnlyList<OfferInfo> Offers,
    IReadOnlyList<AudienceInfo> Audiences,
    IReadOnlyList<string> Channels,
    short MaxPostsPerDay,
    string? PublishWindowStart,
    string? PublishWindowEnd,
    CampaignInfo? Campaign);

public sealed record ProductInfo(Guid Id, string Name, decimal Price, string Currency);
public sealed record OfferInfo(Guid Id, string Title, decimal? DiscountPercent, decimal? OfferPrice, DateTimeOffset EndsAt);
public sealed record AudienceInfo(Guid Id, string Name, string? Description);
public sealed record CampaignInfo(Guid Id, string Name, CampaignObjective Objective, string GoalStatement,
    decimal? Budget, string? BudgetCurrency, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);
