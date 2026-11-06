using SmartMarketingPlatform.DTOs.Auth;

namespace SmartMarketingPlatform.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    // A new access token + a new refresh token; the one sent can no longer be used.
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    // Ends one login session (the refresh token's family).
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    // Ends every session of the user, on every device.
    Task<int> LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken);
}
