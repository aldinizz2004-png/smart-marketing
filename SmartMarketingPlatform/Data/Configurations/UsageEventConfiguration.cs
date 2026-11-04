using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class UsageEventConfiguration : IEntityTypeConfiguration<UsageEvent>
{
    public void Configure(EntityTypeBuilder<UsageEvent> builder)
    {
        builder.ToTable("UsageEvents");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.AiExecution)
            .WithMany()
            .HasForeignKey(x => x.AiExecutionId);

        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Units).HasPrecision(14, 4);
        builder.Property(x => x.EstimatedCostUsd).HasPrecision(14, 6);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
    }
}
