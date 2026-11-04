using SmartMarketingPlatform.DTOs.Social;

namespace SmartMarketingPlatform.Services;

public interface ISocialAccountService
{
    Task<IReadOnlyList<SocialAccountResponse>> GetAllAsync(Guid businessId, CancellationToken cancellationToken);
    Task<SocialAccountResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);

    // Returns whether a new account was created (201) or a disconnected one was reconnected (200).
    Task<(SocialAccountResponse Account, bool Created)> ConnectAsync(Guid businessId, ConnectSocialAccountRequest request, CancellationToken cancellationToken);
    Task<SocialAccountResponse> UpdateCredentialsAsync(Guid businessId, Guid id, UpdateCredentialsRequest request, CancellationToken cancellationToken);
    Task<SocialAccountResponse> DisconnectAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
}
