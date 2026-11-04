using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Configurations;

public class ContentReviewConfiguration : IEntityTypeConfiguration<ContentReview>
{
    public void Configure(EntityTypeBuilder<ContentReview> builder)
    {
        builder.ToTable("ContentReviews");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Revision)
            .WithMany(r => r.Reviews)
            .HasForeignKey(x => x.RevisionId);

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId);

        builder.Property(x => x.Feedback).HasMaxLength(2000);
    }
}
