namespace SmartMarketingPlatform.Entities;

// One table for the statuses of every entity (Campaign, ContentItem, Subscription ...).
public class Status
{
    public int Id { get; set; }                                 // fixed ids, see StatusIds
    public string Category { get; set; } = string.Empty;        // owner entity, e.g. "Campaign"
    public string Code { get; set; } = string.Empty;            // internal identifier, e.g. "Active"
    public string TranslationKey { get; set; } = string.Empty;  // e.g. "status.campaign.active"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}