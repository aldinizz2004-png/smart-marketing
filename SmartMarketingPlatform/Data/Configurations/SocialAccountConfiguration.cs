using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class SocialAccountConfiguration : IEntityTypeConfiguration<SocialAccount>
{
    public void Configure(EntityTypeBuilder<SocialAccount> builder)
    {
        builder.ToTable("SocialAccounts");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.Platform)
            .WithMany()
            .HasForeignKey(x => x.PlatformCode);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.PlatformCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalAccountId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Handle).HasMaxLength(100);
        builder.Property(x => x.CredentialRef).HasMaxLength(300);

        // The same page / account cannot be connected twice.
        builder.HasIndex(x => new { x.PlatformCode, x.ExternalAccountId }).IsUnique();
    }
}
