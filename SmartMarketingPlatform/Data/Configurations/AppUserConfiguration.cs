using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");
        builder.HasKey(x => x.Id);

        // No duplicates: one row per identity and per email.
        builder.Property(x => x.AuthSubject).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.AuthSubject).IsUnique();

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.PasswordHash).HasMaxLength(500);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(5).IsRequired();
    }
}
