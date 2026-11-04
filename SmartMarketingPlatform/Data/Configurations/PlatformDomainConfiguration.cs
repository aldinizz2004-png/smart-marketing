using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class PlatformDomainConfiguration : IEntityTypeConfiguration<PlatformDomain>
{
    public void Configure(EntityTypeBuilder<PlatformDomain> builder)
    {
        builder.ToTable("PlatformDomains");
        builder.HasKey(x => x.Domain);

        builder.Property(x => x.Domain).HasMaxLength(100);
        builder.Property(x => x.PlatformCode).HasMaxLength(30).IsRequired();

        // One-to-many: Platform -> PlatformDomains
        builder.HasOne(x => x.Platform)
            .WithMany(p => p.Domains)
            .HasForeignKey(x => x.PlatformCode);

        builder.HasData(PlatformSeed.Domains);
    }
}