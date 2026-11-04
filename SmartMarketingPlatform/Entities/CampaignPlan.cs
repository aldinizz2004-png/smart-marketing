namespace SmartMarketingPlatform.Entities;

// Versioned AI marketing plan for a campaign (version 1, 2, 3 ...).
public class CampaignPlan
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public int VersionNo { get; set; }
    public string PlanJson { get; set; } = "{}";
    public Guid? GeneratedByExecutionId { get; set; }           // FK -> AiExecution
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public AiExecution? GeneratedByExecution { get; set; }
}
