using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Product aggregate root. Encapsulates product business rules.
/// TenantId is set at creation time and never changed — ensuring
/// a product can never be reassigned to another tenant.
/// </summary>
public class Product : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    // Required by EF Core
    private Product() { }

    public static Product Create(Guid tenantId, string name, string description, decimal price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        return new Product
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            IsActive = true
        };
    }

    public void Update(string name, string description, decimal price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        SetUpdatedAt();
    }

    public void Deactivate() => IsActive = false;
}
