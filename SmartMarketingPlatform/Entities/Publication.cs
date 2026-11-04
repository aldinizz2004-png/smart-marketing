namespace SmartMarketingPlatform.Entities;

// Result of a successful publication job (one-to-one with PublicationJob).
public class Publication
{
    public Guid Id { get; set; }
    public Guid PublicationJobId { get; set; }
    public string? ExternalPostId { get; set; }
    public string? ExternalUrl { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public PublicationJob PublicationJob { get; set; } = null!;
    public ICollection<MetricObservation> Metrics { get; set; } = new List<MetricObservation>();
}
