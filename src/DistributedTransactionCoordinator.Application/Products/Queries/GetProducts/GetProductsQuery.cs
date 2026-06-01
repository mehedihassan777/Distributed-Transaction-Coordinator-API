using MediatR;
using DistributedTransactionCoordinator.Application.Common.DTOs;
using DistributedTransactionCoordinator.Application.Common.Interfaces;
using DistributedTransactionCoordinator.Domain.Interfaces;

namespace DistributedTransactionCoordinator.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to retrieve all products for the current tenant.
/// Queries are read-only operations; they never mutate state.
/// </summary>
public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

/// <summary>
/// Handler for GetProductsQuery.
/// Uses distributed cache (Redis) to avoid redundant DB round-trips.
/// The cache key is tenant-scoped to enforce isolation.
/// </summary>
public sealed class GetProductsQueryHandler(
    IProductRepository productRepository,
    ICacheService cacheService,
    ITenantContext tenantContext)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private static string CacheKey(Guid tenantId) => $"products:tenant:{tenantId}";

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(tenantContext.TenantId);

        var cached = await cacheService.GetAsync<IReadOnlyList<ProductDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var products = await productRepository.GetAllAsync(cancellationToken);

        var dtos = products.Select(p => new ProductDto(
            p.Id,
            p.TenantId,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.IsActive,
            p.CreatedAt,
            p.UpdatedAt)).ToList().AsReadOnly();

        await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);

        return dtos;
    }
}
