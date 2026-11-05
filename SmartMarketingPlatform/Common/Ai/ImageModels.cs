using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

public sealed record ImageInput(string MimeType, byte[] Data, int? Width, int? Height)
{
    public const int MinGoodSide = 800;

    // Measured, not guessed: the same for every model.
    public bool IsLowResolution => Width is { } w && Height is { } h && Math.Min(w, h) < MinGoodSide;
}

// Description: what the photo shows, in words the AI reuses when it writes posts about the product
// (colors, presentation, setting). Advice: one practical tip for the owner when the photo is weak.
public sealed record ImageDescriptionDraft(
    string Description,
    bool ShowsProduct,
    IReadOnlyList<ImageQualityIssue> Issues,
    string? Advice);
