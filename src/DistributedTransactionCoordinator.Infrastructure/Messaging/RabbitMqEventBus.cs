using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using DistributedTransactionCoordinator.Application.Common.Interfaces;

namespace DistributedTransactionCoordinator.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of IEventBus.
/// In production this would be backed by the Outbox pattern worker reading
/// from the outbox_messages table to guarantee at-least-once delivery.
/// </summary>
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "dtc.events";

    public RabbitMqEventBus(IConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var routingKey = typeof(T).Name.ToLowerInvariant();
        var payload = JsonSerializer.SerializeToUtf8Bytes(@event);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(ExchangeName, routingKey, properties, payload);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
