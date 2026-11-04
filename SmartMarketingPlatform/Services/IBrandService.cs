using SmartMarketingPlatform.DTOs.Brand;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

public interface IBrandService
{
    Task<BrandProfileResponse> GetProfileAsync(Guid businessId, CancellationToken cancellationToken);

    // Returns the profile and whether it was created (201) or replaced (200).
    Task<(BrandProfileResponse Profile, bool Created)> UpsertProfileAsync(Guid businessId, BrandProfileRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<BrandTermResponse>> GetTermsAsync(Guid businessId, BrandTermRule? rule, CancellationToken cancellationToken);
    Task<BrandTermResponse> AddTermAsync(Guid businessId, BrandTermRequest request, CancellationToken cancellationToken);
    Task DeleteTermAsync(Guid businessId, Guid termId, CancellationToken cancellationToken);
}
