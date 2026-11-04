using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class BrandTermConfiguration : IEntityTypeConfiguration<BrandTerm>
{
    public void Configure(EntityTypeBuilder<BrandTerm> builder)
    {
        builder.ToTable("BrandTerms");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.BrandProfile)
            .WithMany(p => p.Terms)
            .HasForeignKey(x => x.BusinessId);

        builder.Property(x => x.Term).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Rule).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Note).HasMaxLength(500);

        // The same word cannot be listed twice under the same rule.
        builder.HasIndex(x => new { x.BusinessId, x.Rule, x.Term }).IsUnique();
    }
}
