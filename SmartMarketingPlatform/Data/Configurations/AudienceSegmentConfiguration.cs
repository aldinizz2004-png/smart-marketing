using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class AudienceSegmentConfiguration : IEntityTypeConfiguration<AudienceSegment>
{
    public void Configure(EntityTypeBuilder<AudienceSegment> builder)
    {
        builder.ToTable("AudienceSegments");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.AudienceSegments)
            .HasForeignKey(x => x.BusinessId);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => new { x.BusinessId, x.Name }).IsUnique();
    }
}
