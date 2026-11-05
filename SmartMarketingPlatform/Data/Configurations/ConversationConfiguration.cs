using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId);

        builder.HasOne(x => x.SocialAccount)
            .WithMany()
            .HasForeignKey(x => x.SocialAccountId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.SuggestedByExecution)
            .WithMany()
            .HasForeignKey(x => x.SuggestedByExecutionId);

        builder.Property(x => x.CustomerPhone).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.LastIntent).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.HandoffReason).HasMaxLength(300);
        builder.Property(x => x.SuggestedReply).HasMaxLength(2000);

        // One conversation per customer and per WhatsApp number.
        builder.HasIndex(x => new { x.SocialAccountId, x.CustomerPhone }).IsUnique();
        builder.HasIndex(x => new { x.BusinessId, x.StatusId, x.LastMessageAt });
    }
}
