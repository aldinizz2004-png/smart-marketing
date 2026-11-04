namespace SmartMarketingPlatform.DTOs.Publishing;

// Both optional: the account defaults to the business's connected account on the item's platform,
// the time defaults to the item's planned time (or the first free slot inside the owner's limits).
public sealed class ScheduleRequest
{
    public Guid? SocialAccountId { get; init; }
    public DateTimeOffset? PublishAt { get; init; }
}
