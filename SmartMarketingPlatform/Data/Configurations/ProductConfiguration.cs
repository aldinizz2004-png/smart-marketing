using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.Products)
            .HasForeignKey(x => x.BusinessId);

        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Price).HasPrecision(14, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();

        // SKU is unique inside one business, and only when it is filled.
        builder.HasIndex(x => new { x.BusinessId, x.Sku })
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");
    }
}
