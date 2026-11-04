using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Tenant)
            .WithMany(t => t.Subscriptions)
            .HasForeignKey(x => x.TenantId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        // Self-referencing one-to-one: each period can be renewed at most once.
        builder.HasOne(x => x.RenewedFrom)
            .WithOne(x => x.RenewedBy)
            .HasForeignKey<Subscription>(x => x.RenewedFromSubscriptionId);

        builder.Property(x => x.Duration).HasConversion<byte>();   // stored as 1, 3 or 12
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();

        builder.Property(x => x.PaymentReference).HasMaxLength(200);
        builder.HasIndex(x => x.PaymentReference)
            .IsUnique()
            .HasFilter("[PaymentReference] IS NOT NULL");

        // Fast lookup of the subscription that currently grants access.
        builder.HasIndex(x => new { x.TenantId, x.StatusId, x.EndsAt });
    }
}