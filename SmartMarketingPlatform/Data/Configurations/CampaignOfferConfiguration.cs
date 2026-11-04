using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

// Many-to-many: Campaign <-> Offer
public class CampaignOfferConfiguration : IEntityTypeConfiguration<CampaignOffer>
{
    public void Configure(EntityTypeBuilder<CampaignOffer> builder)
    {
        builder.ToTable("CampaignOffers");
        builder.HasKey(x => new { x.CampaignId, x.OfferId });

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.CampaignOffers)
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.Offer)
            .WithMany()
            .HasForeignKey(x => x.OfferId);
    }
}
