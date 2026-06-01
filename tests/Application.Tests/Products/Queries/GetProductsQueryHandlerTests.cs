using Application.Common.Interfaces;
using Application.Products.Queries.GetProducts;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Application.Tests.Products.Queries;

/// <summary>
/// Unit tests for GetProductsQueryHandler.
/// Tests verify:
///   1. Cache hit returns cached data without hitting the repository.
///   2. Cache miss fetches from repository, populates cache, and returns correct DTOs.
/// </summary>
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetProductsQueryHandler _handler;

    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public GetProductsQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _cacheServiceMock = new Mock<ICacheService>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);

        _handler = new GetProductsQueryHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedProductsWithoutQueryingRepository()
    {
        // Arrange
        var cachedProducts = new List<ProductDto>
        {
            new(Guid.NewGuid(), TenantId, "Cached Widget", "From cache", 9.99m, 10, true, DateTime.UtcNow)
        };

        _cacheServiceMock
            .Setup(c => c.GetAsync<IReadOnlyList<ProductDto>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProducts);

        // Act
        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1, "one product was stored in the cache");
        result[0].Name.Should().Be("Cached Widget");

        _productRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "repository should NOT be queried when a cache hit occurs");
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_FetchesFromRepositoryAndCachesResult()
    {
        // Arrange – cache returns null (miss)
        _cacheServiceMock
            .Setup(c => c.GetAsync<IReadOnlyList<ProductDto>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ProductDto>?)null);

        var products = new List<Product>
        {
            Product.Create(TenantId, "Gizmo", "A handy gizmo", 15.50m, 75),
            Product.Create(TenantId, "Doohickey", "A useful doohickey", 7.25m, 200)
        };

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _cacheServiceMock
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ProductDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2, "two products exist in the repository");
        result.Should().Contain(p => p.Name == "Gizmo" && p.Price == 15.50m);
        result.Should().Contain(p => p.Name == "Doohickey" && p.Price == 7.25m);

        _productRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "repository should be queried exactly once on a cache miss");

        _cacheServiceMock.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ProductDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "results should be cached after a successful repository query");
    }
}
