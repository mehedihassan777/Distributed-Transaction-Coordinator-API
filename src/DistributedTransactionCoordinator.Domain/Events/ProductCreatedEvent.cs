using DistributedTransactionCoordinator.Domain.Common;

namespace DistributedTransactionCoordinator.Domain.Events;

/// <summary>
/// Domain event raised when a new product is created.
/// Consumed by the outbox pattern for reliable async messaging.
/// </summary>
public sealed class ProductCreatedEvent : IDomainEvent
{
    public Guid ProductId { get; }
    public Guid TenantId { get; }
    public string ProductName { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public ProductCreatedEvent(Guid productId, Guid tenantId, string productName)
    {
        ProductId = productId;
        TenantId = tenantId;
        ProductName = productName;
    }
}
