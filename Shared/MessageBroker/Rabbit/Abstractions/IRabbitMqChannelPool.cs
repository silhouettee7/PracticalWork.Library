using RabbitMQ.Client;

namespace MessageBroker.Rabbit.Abstractions;

public interface IRabbitMqChannelPool : IDisposable
{
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
    Task<IChannel> GetChannelForConsumerAsync();
    void ReturnChannel(IChannel channel);
}