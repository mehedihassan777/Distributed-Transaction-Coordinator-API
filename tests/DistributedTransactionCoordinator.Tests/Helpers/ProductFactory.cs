using DistributedTransactionCoordinator.Domain.Entities;
using DistributedTransactionCoordinator.Domain.Interfaces;

namespace DistributedTransactionCoordinator.Tests.Helpers;

/// <summary>
/// Factory for creating valid Product test fixtures.
/// Centralises test data construction to keep test files concise.
/// </summary>
public static class ProductFactory
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Product Create(
        Guid? tenantId = null,
        string name = "Test Product",
        string description = "A product for testing",
        decimal price = 9.99m,
        int stockQuantity = 100)
        => Product.Create(
            tenantId ?? DefaultTenantId,
            name,
            description,
            price,
            stockQuantity);

    /// <summary>
    /// Creates a mock ITenantContext that returns the given tenantId.
    /// </summary>
    public static ITenantContext MockTenantContext(Guid? tenantId = null)
    {
        var mock = new Moq.Mock<ITenantContext>();
        mock.Setup(t => t.TenantId).Returns(tenantId ?? DefaultTenantId);
        return mock.Object;
    }
}
