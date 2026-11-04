using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

// Many-to-many: Campaign <-> Product
public class CampaignProductConfiguration : IEntityTypeConfiguration<CampaignProduct>
{
    public void Configure(EntityTypeBuilder<CampaignProduct> builder)
    {
        builder.ToTable("CampaignProducts");
        builder.HasKey(x => new { x.CampaignId, x.ProductId });

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.CampaignProducts)
            .HasForeignKey(x => x.CampaignId);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId);
    }
}
