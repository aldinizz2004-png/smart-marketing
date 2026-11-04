using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ContentRevisionConfiguration : IEntityTypeConfiguration<ContentRevision>
{
    public void Configure(EntityTypeBuilder<ContentRevision> builder)
    {
        builder.ToTable("ContentRevisions");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ContentItem)
            .WithMany(i => i.Revisions)
            .HasForeignKey(x => x.ContentItemId);

        builder.HasOne(x => x.GeneratedByExecution)
            .WithMany()
            .HasForeignKey(x => x.GeneratedByExecutionId);

        builder.HasOne(x => x.SourceRecommendation)
            .WithMany()
            .HasForeignKey(x => x.SourceRecommendationId);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy);

        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.HookType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.CallToAction).HasConversion<string>().HasMaxLength(30);

        // Each revision number appears once per content item.
        builder.HasIndex(x => new { x.ContentItemId, x.RevisionNo }).IsUnique();
    }
}
