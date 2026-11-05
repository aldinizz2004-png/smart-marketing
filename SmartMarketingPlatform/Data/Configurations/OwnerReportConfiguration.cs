using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class OwnerReportConfiguration : IEntityTypeConfiguration<OwnerReport>
{
    public void Configure(EntityTypeBuilder<OwnerReport> builder)
    {
        builder.ToTable("OwnerReports");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.GeneratedByExecution)
            .WithMany()
            .HasForeignKey(x => x.GeneratedByExecutionId);

        builder.Property(x => x.FactsJson).IsRequired();
        builder.Property(x => x.Headline).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.WinsJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ConcernsJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ActionsJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SentTo).HasMaxLength(1000);

        builder.HasIndex(x => new { x.BusinessId, x.CreatedAt });

        // The weekly worker writes at most one report per business and period, even with two servers.
        builder.HasIndex(x => new { x.BusinessId, x.PeriodEnd })
            .IsUnique()
            .HasFilter("[IsScheduled] = 1")
            .HasDatabaseName("UX_OwnerReports_Business_PeriodEnd_Scheduled");
    }
}
