using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publications");
        builder.HasKey(x => x.Id);

        // One-to-one: a job produces at most one publication.
        builder.HasOne(x => x.PublicationJob)
            .WithOne(j => j.Publication)
            .HasForeignKey<Publication>(x => x.PublicationJobId);

        builder.Property(x => x.ExternalPostId).HasMaxLength(200);
        builder.Property(x => x.ExternalUrl).HasMaxLength(2048);
    }
}
