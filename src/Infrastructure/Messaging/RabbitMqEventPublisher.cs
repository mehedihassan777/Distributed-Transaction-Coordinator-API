using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging;

/// <summary>
/// RabbitMQ-based event publisher implementing the Outbox pattern simulation.
///
/// Architecture note:
/// In a production Outbox pattern, events are first written to a database table
/// inside the same transaction as the domain change, then a background worker
/// reliably delivers them to RabbitMQ. This implementation publishes directly
/// for simplicity; swap the body of PublishAsync for an outbox table write
/// to make it fully durable.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private const string ExchangeName = "domain_events";

    public RabbitMqEventPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var routingKey = typeof(TEvent).Name.ToLowerInvariant();
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = typeof(TEvent).FullName;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(ExchangeName, routingKey, properties, body);

        _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", typeof(TEvent).Name, routingKey);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
