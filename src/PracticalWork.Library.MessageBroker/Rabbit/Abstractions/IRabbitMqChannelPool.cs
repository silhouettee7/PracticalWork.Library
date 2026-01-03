using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Abstractions;

public interface IRabbitMqChannelPool : IDisposable
{
    Task<IChannel> GetChannelAsync();
    Task<IChannel> GetChannelForConsumerAsync();
    void ReturnChannel(IChannel channel);
}