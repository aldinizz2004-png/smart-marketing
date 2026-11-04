using SmartMarketingPlatform.DTOs.Social;

namespace SmartMarketingPlatform.Services;

public interface ISocialConnectionService
{
    Task<ConnectLinkResponse> CreateConnectLinkAsync(Guid businessId, ConnectLinkRequest request, CancellationToken cancellationToken);

    Task<ConnectionResult> CompleteAsync(string? state, string? profileId, bool failed, string? errorCode, CancellationToken cancellationToken);
}
