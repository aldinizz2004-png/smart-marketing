namespace SmartMarketingPlatform.Common.Brand;

// Checks any text (written by a person or by the AI) against the business's prohibited words.
public interface IBrandGuard
{
    Task<IReadOnlyList<string>> FindProhibitedTermsAsync(Guid businessId, string text, CancellationToken cancellationToken);
}
