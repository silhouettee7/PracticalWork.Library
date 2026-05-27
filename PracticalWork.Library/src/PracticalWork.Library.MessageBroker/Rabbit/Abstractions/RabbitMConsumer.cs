using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PracticalWork.Library.MessageBroker.Rabbit.Abstractions;

public abstract class RabbitMConsumer<T>: IRabbitMqConsumer where T: BaseEvent
{
    private IChannel? _channel;
    private string? _consumerTag;
    private readonly IRabbitMqChannelPool _channelPool;
    protected readonly ILogger<RabbitMConsumer<T>> _logger;

    protected RabbitMConsumer(
        ILogger<RabbitMConsumer<T>> logger,
        IRabbitMqChannelPool channelPool)
    {
        _logger = logger;
        _channelPool = channelPool;
    }

    public async Task StartConsuming(string queueName)
    {
        _channel = await _channelPool.GetChannelForConsumerAsync();
        var consumer = new AsyncEventingBasicConsumer(_channel);
            
        consumer.ReceivedAsync += async (_, ea) =>
        {
            await DequeueMessageAsync(ea, queueName);
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,   
            consumer: consumer);
            
        _logger.LogInformation("Начато потребление очереди: {QueueName}", queueName);
    }

    public async Task StopConsuming()
    {
        if (!string.IsNullOrEmpty(_consumerTag) && _channel is not null && _channel.IsOpen)
        {
            await _channel.BasicCancelAsync(_consumerTag);
            _logger.LogInformation("Потребление остановлено");
        }
    }

    private async Task DequeueMessageAsync(BasicDeliverEventArgs ea, string queueName)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var properties = ea.BasicProperties;
            
        _logger.LogDebug(
            "Получено сообщение: Queue={Queue}, MessageId={Id}, DeliveryTag={Tag}",
            queueName, properties.MessageId, ea.DeliveryTag);

        var messageObject = JsonSerializer.Deserialize<T>(message);
        if (_channel is null)
        {
            throw new ArgumentNullException();
        }
        await ProcessMessageAsync(messageObject);
        _logger.LogDebug("Сообщение обработано успешно");
    }

    protected abstract Task ProcessMessageAsync(T? messageObject);
}