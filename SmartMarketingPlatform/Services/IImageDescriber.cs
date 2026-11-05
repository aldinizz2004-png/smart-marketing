using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Services;

public interface IImageDescriber
{
    // Asks the AI to look at the image and saves what it saw on the asset. force = look again.
    Task DescribeAsync(MediaAsset asset, bool force, CancellationToken cancellationToken);
}
