using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class PublicationJobConfiguration : IEntityTypeConfiguration<PublicationJob>
{
    public void Configure(EntityTypeBuilder<PublicationJob> builder)
    {
        builder.ToTable("PublicationJobs");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ContentItem)
            .WithMany()
            .HasForeignKey(x => x.ContentItemId);

        builder.HasOne(x => x.Revision)
            .WithMany()
            .HasForeignKey(x => x.RevisionId);

        builder.HasOne(x => x.ApprovedReview)
            .WithMany()
            .HasForeignKey(x => x.ApprovedReviewId);

        builder.HasOne(x => x.SocialAccount)
            .WithMany()
            .HasForeignKey(x => x.SocialAccountId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.HasOne(x => x.SourceRecommendation)
            .WithMany()
            .HasForeignKey(x => x.SourceRecommendationId);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);

        // The same publish request is never queued twice on the same account.
        builder.HasIndex(x => new { x.SocialAccountId, x.IdempotencyKey }).IsUnique();

        // The publishing worker looks for due jobs by status and time.
        builder.HasIndex(x => new { x.StatusId, x.ScheduledAt });
    }
}
