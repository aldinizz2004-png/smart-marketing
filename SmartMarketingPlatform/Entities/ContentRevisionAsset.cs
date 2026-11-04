namespace SmartMarketingPlatform.Entities;

// Join entity: ContentRevision <-> MediaAsset, with display order.
public class ContentRevisionAsset
{
    public Guid RevisionId { get; set; }
    public Guid AssetId { get; set; }
    public byte Position { get; set; }

    public ContentRevision Revision { get; set; } = null!;
    public MediaAsset Asset { get; set; } = null!;
}
