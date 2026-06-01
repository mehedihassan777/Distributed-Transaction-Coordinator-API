namespace DistributedTransactionCoordinator.Application.Common.Interfaces;

/// <summary>
/// Event bus abstraction for publishing domain events via RabbitMQ.
/// Decouples the Application layer from messaging infrastructure.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
