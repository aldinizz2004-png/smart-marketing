using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("Statuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();   // ids come from StatusIds, not IDENTITY

        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TranslationKey).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => new { x.Category, x.Code }).IsUnique();
        builder.HasIndex(x => x.TranslationKey).IsUnique();

        builder.HasData(StatusSeed.All());
    }
}