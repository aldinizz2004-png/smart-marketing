using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class TrackedSourceConfiguration : IEntityTypeConfiguration<TrackedSource>
{
    public void Configure(EntityTypeBuilder<TrackedSource> builder)
    {
        builder.ToTable("TrackedSources");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Platform)
            .WithMany()
            .HasForeignKey(x => x.PlatformCode);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.AddedByUser)
            .WithMany()
            .HasForeignKey(x => x.AddedBy);

        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.NormalizedUrl).HasMaxLength(800).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PlatformCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.Handle).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        // The same page cannot be tracked twice by one business.
        builder.HasIndex(x => new { x.BusinessId, x.NormalizedUrl }).IsUnique();
    }
}
