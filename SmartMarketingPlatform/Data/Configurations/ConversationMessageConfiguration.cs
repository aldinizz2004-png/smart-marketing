using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(x => x.ConversationId);

        builder.HasOne(x => x.SentByUser)
            .WithMany()
            .HasForeignKey(x => x.SentByUserId);

        builder.HasOne(x => x.AiExecution)
            .WithMany()
            .HasForeignKey(x => x.AiExecutionId);

        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Author).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.ExternalMessageId).HasMaxLength(200);
        builder.Property(x => x.MessageType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.DeliveryStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(500);

        // WhatsApp retries webhooks: the same message is stored once.
        builder.HasIndex(x => x.ExternalMessageId).IsUnique().HasFilter("[ExternalMessageId] IS NOT NULL");
        builder.HasIndex(x => new { x.ConversationId, x.SentAt });

        // The worker looks for customer messages the AI has not read yet.
        builder.HasIndex(x => x.CreatedAt).HasFilter("[ProcessedAt] IS NULL AND [Direction] = 'Inbound'");
    }
}
