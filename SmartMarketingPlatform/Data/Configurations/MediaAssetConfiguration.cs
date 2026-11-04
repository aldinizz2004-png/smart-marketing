using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.MediaAssets)
            .HasForeignKey(x => x.BusinessId);

        // Optional relationship: an asset may or may not belong to a product.
        builder.HasOne(x => x.Product)
            .WithMany(p => p.MediaAssets)
            .HasForeignKey(x => x.ProductId);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength();

        builder.HasIndex(x => x.ObjectKey).IsUnique();
    }
}
