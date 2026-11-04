using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class AiExecutionConfiguration : IEntityTypeConfiguration<AiExecution>
{
    public void Configure(EntityTypeBuilder<AiExecution> builder)
    {
        builder.ToTable("AiExecutions");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EstimatedCostUsd).HasPrecision(14, 6);
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        // Cost / debugging reports by business, task and date.
        builder.HasIndex(x => new { x.BusinessId, x.TaskType, x.CreatedAt });
    }
}
