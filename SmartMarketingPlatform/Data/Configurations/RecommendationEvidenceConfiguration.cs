using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class RecommendationEvidenceConfiguration : IEntityTypeConfiguration<RecommendationEvidence>
{
    public void Configure(EntityTypeBuilder<RecommendationEvidence> builder)
    {
        builder.ToTable("RecommendationEvidence");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Recommendation)
            .WithMany(r => r.Evidence)
            .HasForeignKey(x => x.RecommendationId);

        builder.HasOne(x => x.MetricObservation)
            .WithMany()
            .HasForeignKey(x => x.MetricObservationId);

        builder.HasOne(x => x.SourcePost)
            .WithMany()
            .HasForeignKey(x => x.SourcePostId);

        builder.HasOne(x => x.CompetitorInsight)
            .WithMany()
            .HasForeignKey(x => x.CompetitorInsightId);

        builder.Property(x => x.Interpretation).HasMaxLength(1000);
    }
}
