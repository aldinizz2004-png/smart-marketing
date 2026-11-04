using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Products;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Controllers;

[ApiController]
[Route("api/businesses/{businessId}/products")]
[Authorize(Policy = Policies.ActiveSubscription)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public class ProductsController(IProductService products) : ControllerBase
{
    // GET .../products?search=latte&isActive=true&page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(Guid businessId, [FromQuery] ProductListQuery filter, CancellationToken cancellationToken)
        => Ok(await products.GetAllAsync(businessId, filter, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid businessId, Guid id, CancellationToken cancellationToken)
        => Ok(await products.GetAsync(businessId, id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(Guid businessId, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await products.CreateAsync(businessId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { businessId, id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(Guid businessId, Guid id, ProductRequest request, CancellationToken cancellationToken)
        => Ok(await products.UpdateAsync(businessId, id, request, cancellationToken));

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid businessId, Guid id, CancellationToken cancellationToken)
    {
        await products.DeleteAsync(businessId, id, cancellationToken);
        return NoContent();
    }
}
