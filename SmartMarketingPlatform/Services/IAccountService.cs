using SmartMarketingPlatform.DTOs.Account;

namespace SmartMarketingPlatform.Services;

// The signed-in user's own profile.
public interface IAccountService
{
    Task<MeResponse> GetMeAsync(CancellationToken cancellationToken);
    Task<MeResponse> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken);
}
