using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("ContentItems");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.ContentItems)
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.Platform)
            .WithMany()
            .HasForeignKey(x => x.PlatformCode);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.SourceRecommendation)
            .WithMany()
            .HasForeignKey(x => x.SourceRecommendationId);

        builder.HasOne(x => x.InspiredBySourcePost)
            .WithMany()
            .HasForeignKey(x => x.InspiredBySourcePostId);

        builder.Property(x => x.PlatformCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ContentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.VariantLabel).HasMaxLength(10);
    }
}
