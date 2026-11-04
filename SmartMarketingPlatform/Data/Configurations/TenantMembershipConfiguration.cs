using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships");

        // Composite primary key: a user appears once per tenant.
        builder.HasKey(x => new { x.TenantId, x.UserId });

        builder.HasOne(x => x.Tenant)
            .WithMany(t => t.Memberships)
            .HasForeignKey(x => x.TenantId);

        builder.HasOne(x => x.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        // Enum stored as readable text instead of a number.
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
    }
}