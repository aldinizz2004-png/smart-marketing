using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class AutoApplyRuleConfiguration : IEntityTypeConfiguration<AutoApplyRule>
{
    public void Configure(EntityTypeBuilder<AutoApplyRule> builder)
    {
        builder.ToTable("AutoApplyRules");

        // Composite key: each action type appears once per business.
        builder.HasKey(x => new { x.BusinessId, x.ActionType });

        builder.HasOne(x => x.Policy)
            .WithMany(p => p.AutoApplyRules)
            .HasForeignKey(x => x.BusinessId);

        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(30);
    }
}
