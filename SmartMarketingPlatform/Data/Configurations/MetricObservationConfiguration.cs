using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class MetricObservationConfiguration : IEntityTypeConfiguration<MetricObservation>
{
    public void Configure(EntityTypeBuilder<MetricObservation> builder)
    {
        builder.ToTable("MetricObservations");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Publication)
            .WithMany(p => p.Metrics)
            .HasForeignKey(x => x.PublicationId);

        builder.Property(x => x.MetricCode).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.MetricValue).HasPrecision(20, 4);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);

        // One snapshot per metric per moment (collectors can safely retry).
        builder.HasIndex(x => new { x.PublicationId, x.MetricCode, x.ObservedAt }).IsUnique();
    }
}
