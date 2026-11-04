using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class AutomationPolicyConfiguration : IEntityTypeConfiguration<AutomationPolicy>
{
    public void Configure(EntityTypeBuilder<AutomationPolicy> builder)
    {
        builder.ToTable("AutomationPolicies");
        builder.HasKey(x => x.BusinessId);

        // One-to-one with Business (no navigation needed on the Business side).
        builder.HasOne(x => x.Business)
            .WithOne()
            .HasForeignKey<AutomationPolicy>(x => x.BusinessId);

        builder.HasOne(x => x.UpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy);

        builder.Property(x => x.MaxDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.MonthlyAdBudget).HasPrecision(14, 2);
        builder.Property(x => x.BudgetCurrency).HasMaxLength(3).IsFixedLength();
    }
}
