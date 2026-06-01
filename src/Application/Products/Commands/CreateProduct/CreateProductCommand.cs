using MediatR;

namespace Application.Products.Commands.CreateProduct;

/// <summary>
/// Command (write side of CQRS) to create a new Product for the current tenant.
/// Commands are immutable records — they represent the intent to change state.
/// The TenantId is injected by the handler from ITenantService, not trusted from the caller.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity) : IRequest<Guid>;
