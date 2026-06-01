using DistributedTransactionCoordinator.Domain.Common;

namespace DistributedTransactionCoordinator.Domain.Entities;

/// <summary>
/// Product aggregate root.
/// Encapsulates business rules and raises domain events on state changes.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core requires a parameterless constructor
    private Product() { }

    public static Product Create(Guid tenantId, string name, string description, decimal price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (price < 0) throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (stockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        var product = new Product
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity
        };

        product.AddDomainEvent(new Events.ProductCreatedEvent(product.Id, tenantId, name));
        return product;
    }

    public void Update(string name, string description, decimal price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (price < 0) throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (stockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}
