using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Entities;

// A snapshot of one metric of one of OUR publications at a moment in time.
public class MetricObservation
{
    public Guid Id { get; set; }
    public Guid PublicationId { get; set; }
    public MetricCode MetricCode { get; set; }
    public decimal MetricValue { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public MetricSource Source { get; set; }
    public string? RawPayloadJson { get; set; }

    public Publication Publication { get; set; } = null!;
}
