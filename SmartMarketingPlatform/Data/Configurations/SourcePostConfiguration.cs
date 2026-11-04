using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class SourcePostConfiguration : IEntityTypeConfiguration<SourcePost>
{
    public void Configure(EntityTypeBuilder<SourcePost> builder)
    {
        builder.ToTable("SourcePosts");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.TrackedSource)
            .WithMany(s => s.Posts)
            .HasForeignKey(x => x.TrackedSourceId);

        builder.Property(x => x.ExternalPostId).HasMaxLength(200);
        builder.Property(x => x.PostUrl).HasMaxLength(2048);
        builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.HookType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.EngagementRate).HasPrecision(9, 6);

        // A post is stored once per source; re-scans update the same row.
        builder.HasIndex(x => new { x.TrackedSourceId, x.ExternalPostId })
            .IsUnique()
            .HasFilter("[ExternalPostId] IS NOT NULL");
    }
}
