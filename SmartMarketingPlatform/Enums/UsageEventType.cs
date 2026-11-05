namespace SmartMarketingPlatform.Enums;

// Stored as text, so adding a value needs no migration.
public enum UsageEventType
{
    CopyGeneration,
    ImageGeneration,
    VideoGeneration,
    Publish,
    CompetitorScan,
    StrategyGeneration,     // campaign plans and strategy runs
    CommentAnalysis,
    OwnerReport,
    CompetitorAnalysis,
    CustomerReply,          // the AI reading a WhatsApp conversation
    WhatsAppMessage,        // a message we sent (Meta charges per conversation)
    ImageDescription        // the AI looking at a photo
}
