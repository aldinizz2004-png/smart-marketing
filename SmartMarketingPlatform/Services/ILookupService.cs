using SmartMarketingPlatform.DTOs.Lookups;

namespace SmartMarketingPlatform.Services;

// Read-only reference data: statuses and platforms, translated to the request language.
public interface ILookupService
{
    Task<IReadOnlyList<StatusResponse>> GetStatusesAsync(string category, string language, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlatformResponse>> GetPlatformsAsync(string language, CancellationToken cancellationToken);
    Task<PlatformResponse> GetPlatformAsync(string code, string language, CancellationToken cancellationToken);
}
