using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> builder)
    {
        builder.ToTable("Platforms");
        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code).HasMaxLength(30);
        builder.Property(x => x.DisplayNameKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.DisplayNameKey).IsUnique();

        builder.HasData(PlatformSeed.Platforms);
    }
}