using Application.Common.Interfaces;
using Application.Products.Commands.CreateProduct;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Application.Tests.Products.Commands;

/// <summary>
/// Unit tests for CreateProductCommandHandler.
/// Tests follow strict Arrange-Act-Assert (AAA) structure.
/// All external dependencies (UnitOfWork, TenantService, EventPublisher)
/// are mocked so tests run in isolation with no DB or infrastructure.
/// </summary>
public class CreateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateProductCommandHandler _handler;

    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public CreateProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _tenantServiceMock = new Mock<ITenantService>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        // Wire up the mock repository through UnitOfWork
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tenantServiceMock.Setup(t => t.TenantId).Returns(TestTenantId);

        _eventPublisherMock
            .Setup(e => e.PublishAsync(It.IsAny<ProductCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateProductCommandHandler(
            _unitOfWorkMock.Object,
            _tenantServiceMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewProductId()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Widget Pro",
            Description: "A high-quality widget",
            Price: 29.99m,
            StockQuantity: 100);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => capturedProduct = product)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty("a new product Guid should be returned on successful creation");
        capturedProduct.Should().NotBeNull("the product should have been added to the repository");
        capturedProduct!.Name.Should().Be("Widget Pro");
        capturedProduct.Price.Should().Be(29.99m);
        capturedProduct.TenantId.Should().Be(TestTenantId, "TenantId must be sourced from ITenantService");
        capturedProduct.IsActive.Should().BeTrue("newly created products are active by default");
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesProductCreatedEvent()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Gadget Elite",
            Description: "Premium gadget",
            Price: 199.99m,
            StockQuantity: 50);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => capturedProduct = product)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert – verify the integration event was published with correct data
        _eventPublisherMock.Verify(
            e => e.PublishAsync(
                It.Is<ProductCreatedEvent>(ev =>
                    ev.TenantId == TestTenantId &&
                    ev.ProductName == "Gadget Elite"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "a ProductCreatedEvent must be published exactly once after a product is created");
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChangesExactlyOnce()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Component X",
            Description: "Test component",
            Price: 5.00m,
            StockQuantity: 200);

        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "SaveChangesAsync must be called exactly once per command to commit the unit of work");
    }
}
