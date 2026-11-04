using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.Offers)
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.OfferPrice).HasPrecision(14, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
    }
}
