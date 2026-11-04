namespace SmartMarketingPlatform.Entities;

// Why a recommendation was made. One of the three links is set:
// one of our metrics, a competitor post, or a competitor insight.
public class RecommendationEvidence
{
    public Guid Id { get; set; }
    public Guid RecommendationId { get; set; }
    public Guid? MetricObservationId { get; set; }
    public Guid? SourcePostId { get; set; }
    public Guid? CompetitorInsightId { get; set; }
    public string? Interpretation { get; set; }

    public StrategyRecommendation Recommendation { get; set; } = null!;
    public MetricObservation? MetricObservation { get; set; }
    public SourcePost? SourcePost { get; set; }
    public CompetitorInsight? CompetitorInsight { get; set; }
}
