using System.ComponentModel.DataAnnotations;

namespace SmartMarketingPlatform.DTOs.Tenants;

public sealed class UpdateTenantRequest
{
    [Required, MaxLength(200)]
    public string? Name { get; init; }
}
