using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

// Many-to-many: Campaign <-> AudienceSegment
public class CampaignAudienceConfiguration : IEntityTypeConfiguration<CampaignAudience>
{
    public void Configure(EntityTypeBuilder<CampaignAudience> builder)
    {
        builder.ToTable("CampaignAudiences");
        builder.HasKey(x => new { x.CampaignId, x.AudienceSegmentId });

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.CampaignAudiences)
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.AudienceSegment)
            .WithMany()
            .HasForeignKey(x => x.AudienceSegmentId);
    }
}
