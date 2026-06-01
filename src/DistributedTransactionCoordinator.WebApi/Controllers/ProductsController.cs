using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DistributedTransactionCoordinator.Application.Common.DTOs;
using DistributedTransactionCoordinator.Application.Products.Commands.CreateProduct;
using DistributedTransactionCoordinator.Application.Products.Queries.GetProducts;

namespace DistributedTransactionCoordinator.WebApi.Controllers;

/// <summary>
/// Products API controller.
/// Thin controller — all business logic is delegated to MediatR handlers.
/// Controllers are responsible only for HTTP concern mapping.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all active products for the current tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product for the current tenant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var productId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProducts), new { id = productId }, productId);
    }
}
