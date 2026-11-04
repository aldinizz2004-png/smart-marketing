namespace SmartMarketingPlatform.Common.Ai;

public interface IMarketingContextBuilder
{
    // campaignId = null: the whole business (active products, running offers ...).
    Task<MarketingContext> BuildAsync(Guid businessId, Guid? campaignId, CancellationToken cancellationToken);
}
