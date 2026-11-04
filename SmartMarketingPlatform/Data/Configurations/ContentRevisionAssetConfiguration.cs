using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

// Many-to-many: ContentRevision <-> MediaAsset
public class ContentRevisionAssetConfiguration : IEntityTypeConfiguration<ContentRevisionAsset>
{
    public void Configure(EntityTypeBuilder<ContentRevisionAsset> builder)
    {
        builder.ToTable("ContentRevisionAssets");
        builder.HasKey(x => new { x.RevisionId, x.AssetId });

        builder.HasOne(x => x.Revision)
            .WithMany(r => r.Assets)
            .HasForeignKey(x => x.RevisionId);

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.AssetId);
    }
}
