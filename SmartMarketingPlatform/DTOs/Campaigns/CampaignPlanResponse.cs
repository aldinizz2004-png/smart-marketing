using System.Text.Json;

namespace SmartMarketingPlatform.DTOs.Campaigns;

// One version of the AI plan. Older versions are kept (history).
public sealed record CampaignPlanResponse(
    Guid Id,
    Guid CampaignId,
    int VersionNo,
    JsonElement Plan,
    Guid? GeneratedByExecutionId,
    DateTimeOffset CreatedAt);
