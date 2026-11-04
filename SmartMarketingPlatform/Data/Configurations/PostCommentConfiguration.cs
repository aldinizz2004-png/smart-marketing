using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("PostComments");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Publication)
            .WithMany(p => p.Comments)
            .HasForeignKey(x => x.PublicationId);

        builder.HasOne(x => x.AnalyzedByExecution)
            .WithMany()
            .HasForeignKey(x => x.AnalyzedByExecutionId);

        builder.HasOne(x => x.RepliedByUser)
            .WithMany()
            .HasForeignKey(x => x.RepliedByUserId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.ExternalCommentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParentExternalId).HasMaxLength(200);
        builder.Property(x => x.AuthorExternalId).HasMaxLength(200);
        builder.Property(x => x.AuthorName).HasMaxLength(200);
        builder.Property(x => x.AuthorUsername).HasMaxLength(100);
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.SuggestedReply).HasMaxLength(2000);
        builder.Property(x => x.ReplyBody).HasMaxLength(2000);

        builder.HasIndex(x => new { x.PublicationId, x.ExternalCommentId }).IsUnique();
        builder.HasIndex(x => new { x.StatusId, x.PostedAt });
    }
}
