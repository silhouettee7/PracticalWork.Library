using System.Text;
using System.Text.Json;
using Domain.Abstractions.MessageBroker;
using MessageBroker.Rabbit.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MessageBroker.Rabbit.Publishers;

public class RabbitMqPublisher: IRabbitMqPublisher
{
    private readonly IRabbitMqChannelPool _channelPool;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IRabbitMqChannelPool channelPool,
        ILogger<RabbitMqPublisher> logger)
    {
        _channelPool = channelPool;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        IChannel? channel = null;
        
        try
        {
            channel = await _channelPool.GetChannelAsync(cancellationToken);
            
            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(message));
            
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка публикации: Exchange={Exchange}, RoutingKey={Key}",
                exchange, routingKey);
            return false;
        }
        finally
        {
            if (channel != null)
                _channelPool.ReturnChannel(channel);
        }
    }
}