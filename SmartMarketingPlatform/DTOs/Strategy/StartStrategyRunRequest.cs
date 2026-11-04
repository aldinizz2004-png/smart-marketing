using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Strategy;

// Ask the AI what to do next, for the whole business or for one campaign.
public sealed class StartStrategyRunRequest
{
    public Guid? CampaignId { get; init; }

    [Range(1, 30)]
    public int WindowDays { get; init; } = 7;
}
