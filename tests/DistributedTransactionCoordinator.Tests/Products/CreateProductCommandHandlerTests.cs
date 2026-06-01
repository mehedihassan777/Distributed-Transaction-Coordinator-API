using FluentAssertions;
using Moq;
using DistributedTransactionCoordinator.Application.Products.Commands.CreateProduct;
using DistributedTransactionCoordinator.Domain.Entities;
using DistributedTransactionCoordinator.Domain.Interfaces;
using DistributedTransactionCoordinator.Tests.Helpers;

namespace DistributedTransactionCoordinator.Tests.Products;

/// <summary>
/// Unit tests for CreateProductCommandHandler.
/// Each test follows the strict Arrange-Act-Assert (AAA) pattern.
/// Infrastructure dependencies (repository, UoW, tenant context) are mocked with Moq.
/// </summary>
public class CreateProductCommandHandlerTests
{
    // ── Shared mocks ──────────────────────────────────────────────────────────
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly CreateProductCommandHandler _sut;

    public CreateProductCommandHandlerTests()
    {
        _tenantContextMock.Setup(t => t.TenantId).Returns(ProductFactory.DefaultTenantId);
        _sut = new CreateProductCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _tenantContextMock.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: Happy path — valid command produces a new product Id
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewProductId()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Widget Pro",
            Description: "A high-quality widget",
            Price: 49.99m,
            StockQuantity: 250);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty(because: "a newly created product must have a non-empty Id");

        _repositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Product>(p =>
                    p.Name == command.Name &&
                    p.Price == command.Price &&
                    p.TenantId == ProductFactory.DefaultTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "the product must be added to the repository exactly once");

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "changes must be committed to the database exactly once");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: Tenant isolation — product is stamped with the current tenant Id
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_ValidCommand_StampsProductWithCurrentTenantId()
    {
        // Arrange
        var expectedTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        _tenantContextMock.Setup(t => t.TenantId).Returns(expectedTenantId);

        var sut = new CreateProductCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _tenantContextMock.Object);

        var command = new CreateProductCommand(
            Name: "Tenant-Scoped Item",
            Description: "Must be linked to the correct tenant",
            Price: 19.99m,
            StockQuantity: 10);

        Product? capturedProduct = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        capturedProduct.Should().NotBeNull(because: "AddAsync must be called with a Product instance");
        capturedProduct!.TenantId.Should().Be(expectedTenantId,
            because: "the product's TenantId must match the current tenant context");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: Cancellation propagation — CancellationToken is forwarded correctly
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Handle_ValidCommand_PropagatesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var command = new CreateProductCommand("Item", "Desc", 1m, 1);

        CancellationToken capturedTokenForAdd = default;
        CancellationToken capturedTokenForSave = default;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((_, ct) => capturedTokenForAdd = ct)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => capturedTokenForSave = ct)
            .ReturnsAsync(1);

        // Act
        await _sut.Handle(command, token);

        // Assert
        capturedTokenForAdd.Should().Be(token,
            because: "the CancellationToken must be forwarded to AddAsync");
        capturedTokenForSave.Should().Be(token,
            because: "the CancellationToken must be forwarded to SaveChangesAsync");
    }
}
