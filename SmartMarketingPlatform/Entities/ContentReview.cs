namespace SmartMarketingPlatform.Entities;

// The owner's (or a reviewer's) decision on one exact revision.
public class ContentReview
{
    public Guid Id { get; set; }
    public Guid RevisionId { get; set; }
    public Guid ReviewerId { get; set; }                        // FK -> AppUser
    public int StatusId { get; set; }                           // Approved / Rejected / ChangesRequested
    public string? Feedback { get; set; }
    public DateTimeOffset DecidedAt { get; set; }

    public ContentRevision Revision { get; set; } = null!;
    public AppUser Reviewer { get; set; } = null!;
    public Status Status { get; set; } = null!;
}
