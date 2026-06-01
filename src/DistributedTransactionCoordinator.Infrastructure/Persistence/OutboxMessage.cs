using System.Text.Json;
using DistributedTransactionCoordinator.Domain.Common;

namespace DistributedTransactionCoordinator.Infrastructure.Persistence;

/// <summary>
/// Outbox pattern: domain events are stored atomically with the business transaction,
/// then a background worker picks them up and publishes to RabbitMQ.
/// This guarantees at-least-once delivery without distributed transactions.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }

    private OutboxMessage() { }

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent) => new()
    {
        EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
    };
}
