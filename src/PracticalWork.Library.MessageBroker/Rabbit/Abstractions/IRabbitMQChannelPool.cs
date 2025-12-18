using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Abstractions;

public interface IRabbitMQChannelPool : IDisposable
{
    Task<IChannel> GetChannelAsync();
    Task<IChannel> GetChannelForConsumerAsync();
    void ReturnChannel(IChannel channel);
}