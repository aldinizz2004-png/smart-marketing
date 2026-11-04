using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class StrategyRecommendationConfiguration : IEntityTypeConfiguration<StrategyRecommendation>
{
    public void Configure(EntityTypeBuilder<StrategyRecommendation> builder)
    {
        builder.ToTable("StrategyRecommendations");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.StrategyRun)
            .WithMany(r => r.Recommendations)
            .HasForeignKey(x => x.StrategyRunId);

        builder.HasOne(x => x.Campaign)
            .WithMany()
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.DecidedByUser)
            .WithMany()
            .HasForeignKey(x => x.DecidedBy);

        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ProposedPayloadJson).IsRequired();
        builder.Property(x => x.Rationale).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Confidence).HasPrecision(4, 3);
    }
}
