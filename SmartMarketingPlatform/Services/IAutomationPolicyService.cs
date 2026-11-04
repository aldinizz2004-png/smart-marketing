using SmartMarketingPlatform.DTOs.Automation;

namespace SmartMarketingPlatform.Services;

public interface IAutomationPolicyService
{
    Task<AutomationPolicyResponse> GetAsync(Guid businessId, CancellationToken cancellationToken);
    Task<(AutomationPolicyResponse Policy, bool Created)> UpsertAsync(Guid businessId, AutomationPolicyRequest request, CancellationToken cancellationToken);
}
