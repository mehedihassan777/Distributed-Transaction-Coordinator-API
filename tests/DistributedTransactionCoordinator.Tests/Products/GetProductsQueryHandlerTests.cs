using FluentAssertions;
using Moq;
using DistributedTransactionCoordinator.Application.Common.DTOs;
using DistributedTransactionCoordinator.Application.Common.Interfaces;
using DistributedTransactionCoordinator.Application.Products.Queries.GetProducts;
using DistributedTransactionCoordinator.Domain.Interfaces;
using DistributedTransactionCoordinator.Tests.Helpers;

namespace DistributedTransactionCoordinator.Tests.Products;

/// <summary>
/// Unit tests for GetProductsQueryHandler.
/// Verifies cache-hit, cache-miss (DB fallback), and tenant-scoped cache key isolation.
/// </summary>
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly GetProductsQueryHandler _sut;

    public GetProductsQueryHandlerTests()
    {
        _tenantContextMock.Setup(t => t.TenantId).Returns(ProductFactory.DefaultTenantId);
        _sut = new GetProductsQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _tenantContextMock.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: Cache miss — falls back to repository and caches the result
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesRepositoryAndPopulatesCache()
    {
        // Arrange
        var products = new List<Domain.Entities.Product>
        {
            ProductFactory.Create(name: "Alpha"),
            ProductFactory.Create(name: "Beta")
        }.AsReadOnly();

        // Cache returns null → simulates a cache miss
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ProductDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ProductDto>?)null);

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ProductDto>>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2, because: "two products were returned by the repository");
        result.Should().Contain(p => p.Name == "Alpha");
        result.Should().Contain(p => p.Name == "Beta");

        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "the repository must be queried once on cache miss");

        _cacheMock.Verify(
            c => c.SetAsync(
                It.Is<string>(k => k.Contains(ProductFactory.DefaultTenantId.ToString())),
                It.IsAny<IReadOnlyList<ProductDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "results must be stored in the tenant-scoped cache after a miss");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: Cache hit — returns cached data without hitting the repository
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedDataWithoutQueryingRepository()
    {
        // Arrange
        var cachedDtos = new List<ProductDto>
        {
            new(Guid.NewGuid(), ProductFactory.DefaultTenantId, "Cached Product",
                "From cache", 5.00m, 50, true, DateTime.UtcNow, null)
        }.AsReadOnly();

        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ProductDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDtos);

        // Act
        var result = await _sut.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1, because: "the cached list contains one item");
        result[0].Name.Should().Be("Cached Product", because: "the cached DTO must be returned as-is");

        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "the repository must NOT be called when data is already in the cache");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: Tenant isolation — cache key contains the tenant Id
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_CacheKey_IsScopedToCurrentTenant()
    {
        // Arrange
        var tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var sut = new GetProductsQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _tenantContextMock.Object);

        string? capturedKey = null;
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ProductDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .ReturnsAsync((IReadOnlyList<ProductDto>?)null);

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Product>().AsReadOnly());

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ProductDto>>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await sut.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        capturedKey.Should().NotBeNull();
        capturedKey.Should().Contain(tenantId.ToString(),
            because: "the cache key must embed the tenant Id to prevent cross-tenant data leakage");
    }
}
