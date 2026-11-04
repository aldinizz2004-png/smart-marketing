using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class StrategyRunConfiguration : IEntityTypeConfiguration<StrategyRun>
{
    public void Configure(EntityTypeBuilder<StrategyRun> builder)
    {
        builder.ToTable("StrategyRuns");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.AiExecution)
            .WithMany()
            .HasForeignKey(x => x.AiExecutionId);

        builder.Property(x => x.AlgorithmVersion).HasMaxLength(50).IsRequired();
    }
}
