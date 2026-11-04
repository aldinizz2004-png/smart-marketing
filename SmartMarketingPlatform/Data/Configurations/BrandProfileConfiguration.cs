using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class BrandProfileConfiguration : IEntityTypeConfiguration<BrandProfile>
{
    public void Configure(EntityTypeBuilder<BrandProfile> builder)
    {
        builder.ToTable("BrandProfiles");
        builder.HasKey(x => x.BusinessId);

        // One-to-one: the primary key is also the foreign key to Business.
        builder.HasOne(x => x.Business)
            .WithOne(b => b.BrandProfile)
            .HasForeignKey<BrandProfile>(x => x.BusinessId);

        builder.Property(x => x.Dialect).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Tone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Guidelines).HasMaxLength(4000);
    }
}
