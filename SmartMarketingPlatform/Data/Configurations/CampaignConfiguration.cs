using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Objective).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.GoalStatement).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TargetMetric).HasMaxLength(50);
        builder.Property(x => x.TargetValue).HasPrecision(18, 4);
        builder.Property(x => x.Budget).HasPrecision(14, 2);
        builder.Property(x => x.BudgetCurrency).HasMaxLength(3).IsFixedLength();
    }
}
