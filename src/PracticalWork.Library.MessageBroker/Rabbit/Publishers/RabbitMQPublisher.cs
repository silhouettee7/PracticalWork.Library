using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Publishers;

public class RabbitMQPublisher: IRabbitMQPublisher
{
    private readonly IRabbitMQChannelPool _channelPool;
    private readonly ILogger<RabbitMQPublisher> _logger;

    public RabbitMQPublisher(
        IRabbitMQChannelPool channelPool,
        ILogger<RabbitMQPublisher> logger)
    {
        _channelPool = channelPool;
        _logger = logger;
    }
    public async Task<bool> PublishAsync<T>(string exchange, string routingKey, T message)
    {
        IChannel? channel = null;
        
        try
        {
            channel = await _channelPool.GetChannelAsync();
            
            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(message));
            
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body);
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