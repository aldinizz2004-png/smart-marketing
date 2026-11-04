using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class CampaignPlanConfiguration : IEntityTypeConfiguration<CampaignPlan>
{
    public void Configure(EntityTypeBuilder<CampaignPlan> builder)
    {
        builder.ToTable("CampaignPlans");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.Plans)
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.GeneratedByExecution)
            .WithMany()
            .HasForeignKey(x => x.GeneratedByExecutionId);

        builder.Property(x => x.PlanJson).IsRequired();

        // Each version number appears once per campaign.
        builder.HasIndex(x => new { x.CampaignId, x.VersionNo }).IsUnique();
    }
}
