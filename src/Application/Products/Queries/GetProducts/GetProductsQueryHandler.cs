using Application.Common.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Products.Queries.GetProducts;

/// <summary>
/// Handles the GetProductsQuery.
/// Leverages the repository which inherits EF Core's global query filter,
/// meaning the RLS is transparently enforced — no manual tenant filtering needed.
/// Optionally reads from Redis cache to avoid repeated DB hits.
/// </summary>
public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    private const string CacheKeyPrefix = "products:tenant:";

    public GetProductsQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        // Attempt to serve from distributed cache first
        var cached = await _cacheService.GetAsync<IReadOnlyList<ProductDto>>(
            CacheKeyPrefix, cancellationToken);

        if (cached is not null)
            return cached;

        var products = await _unitOfWork.Products.GetAllAsync(cancellationToken);

        var dtos = products.Select(p => new ProductDto(
            p.Id,
            p.TenantId,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.IsActive,
            p.CreatedAt)).ToList();

        await _cacheService.SetAsync(CacheKeyPrefix, (IReadOnlyList<ProductDto>)dtos,
            TimeSpan.FromMinutes(5), cancellationToken);

        return dtos;
    }
}
