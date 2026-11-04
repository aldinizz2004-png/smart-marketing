using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace SmartMarketingPlatform.Common.Auth;

// When the ONLY reason a request is refused is a missing subscription, answer
// 402 Payment Required instead of 403, so the client knows to send the user to the payment page.
// Every other case (401, 403) keeps the default behaviour.
public sealed class ProblemDetailsAuthorizationResultHandler(IProblemDetailsService problemDetails)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var failed = authorizeResult.AuthorizationFailure?.FailedRequirements.ToList();
        if (authorizeResult.Forbidden && failed is { Count: > 0 } && failed.All(r => r is SubscriptionRequirement))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status402PaymentRequired,
                    Title = "Payment required",
                    Detail = "This feature needs an active subscription. See /api/tenant/subscriptions."
                }
            });
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
