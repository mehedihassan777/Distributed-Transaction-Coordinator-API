namespace Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing domain events to the message bus (RabbitMQ).
/// Supports the Outbox pattern: handlers publish events through this interface,
/// and the Infrastructure layer handles durable delivery.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
