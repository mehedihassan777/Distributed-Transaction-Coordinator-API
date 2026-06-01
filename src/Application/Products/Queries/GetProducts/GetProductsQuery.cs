using MediatR;

namespace Application.Products.Queries.GetProducts;

/// <summary>
/// Query (read side of CQRS) to retrieve all products for the current tenant.
/// No payload needed — the tenant filter is applied globally by EF Core's
/// query filter, so the handler just fetches all visible products.
/// </summary>
public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
