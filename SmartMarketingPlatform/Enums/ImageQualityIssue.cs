namespace SmartMarketingPlatform.Enums;

// What makes a photo weak for an ad. LowResolution is measured by code; the others are seen by the AI.
public enum ImageQualityIssue
{
    LowResolution,
    Blurry,
    Dark,
    Overexposed,
    ClutteredBackground,
    ProductNotVisible,
    TextUnreadable
}
