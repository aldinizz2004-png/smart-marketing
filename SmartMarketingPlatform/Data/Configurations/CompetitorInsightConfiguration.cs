using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class CompetitorInsightConfiguration : IEntityTypeConfiguration<CompetitorInsight>
{
    public void Configure(EntityTypeBuilder<CompetitorInsight> builder)
    {
        builder.ToTable("CompetitorInsights");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        // Optional: null = insight across all tracked sources.
        builder.HasOne(x => x.TrackedSource)
            .WithMany(s => s.Insights)
            .HasForeignKey(x => x.TrackedSourceId);

        builder.Property(x => x.InsightType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Confidence).HasPrecision(4, 3);
    }
}
