using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

// Many-to-many: AutomationPolicy <-> Platform through an explicit join entity.
public class AutomationPolicyPlatformConfiguration : IEntityTypeConfiguration<AutomationPolicyPlatform>
{
    public void Configure(EntityTypeBuilder<AutomationPolicyPlatform> builder)
    {
        builder.ToTable("AutomationPolicyPlatforms");
        builder.HasKey(x => new { x.BusinessId, x.PlatformCode });

        builder.HasOne(x => x.Policy)
            .WithMany(p => p.AllowedPlatforms)
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Platform)
            .WithMany()
            .HasForeignKey(x => x.PlatformCode);

        builder.Property(x => x.PlatformCode).HasMaxLength(30);
    }
}
