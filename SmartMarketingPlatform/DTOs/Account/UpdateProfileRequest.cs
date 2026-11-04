using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Account;

// PATCH: only the fields that are sent are changed; null means "leave as is".
public sealed class UpdateProfileRequest
{
    [MinLength(1), MaxLength(200)]
    public string? FullName { get; init; }

    [RegularExpression("^(ar|en)$", ErrorMessage = "PreferredLanguage must be 'ar' or 'en'.")]
    public string? PreferredLanguage { get; init; }
}
