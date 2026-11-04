namespace SmartMarketingPlatform.Enums;

// Stored as text, so adding a value needs no migration.
public enum UsageEventType
{
    CopyGeneration,
    ImageGeneration,
    VideoGeneration,
    Publish,
    CompetitorScan,
    StrategyGeneration      // campaign plans and strategy runs
}
