using Application.Products.Commands.CreateProduct;
using Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// REST API controller for Product operations.
/// Thin by design — delegates all business logic to MediatR commands/queries.
/// Every request is tenant-scoped through the global JWT/TenantMiddleware pipeline.
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
    /// Retrieves all products for the authenticated tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product for the authenticated tenant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity);

        var productId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = productId }, productId);
    }
}

/// <summary>
/// Request body for creating a product. Kept separate from the command
/// to avoid exposing internal types in the API contract.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);
