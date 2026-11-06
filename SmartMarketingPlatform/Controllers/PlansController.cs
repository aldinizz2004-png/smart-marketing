using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.DTOs.Subscriptions;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

// Public: the pricing page shows the plans before anyone signs up.
[ApiController]
[Route("api/plans")]
[AllowAnonymous]
public class PlansController(ISubscriptionService subscriptions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PlanResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PlanResponse>> GetAll() => Ok(subscriptions.GetPlans());
}
