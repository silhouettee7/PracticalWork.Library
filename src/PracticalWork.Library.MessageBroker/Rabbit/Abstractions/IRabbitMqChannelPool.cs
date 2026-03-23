using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Abstractions;

public interface IRabbitMqChannelPool : IDisposable
{
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
    Task<IChannel> GetChannelForConsumerAsync();
    void ReturnChannel(IChannel channel);
}