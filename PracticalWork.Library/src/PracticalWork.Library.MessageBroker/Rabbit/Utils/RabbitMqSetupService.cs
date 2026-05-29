using Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using PracticalWork.Library.Options;
using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Utils;

public class RabbitMqSetupService
{
    private readonly IRabbitMqChannelPool _channelPool;
    private readonly ILogger<RabbitMqSetupService> _logger;
    private readonly RabbitOptions _rabbitOptions;
    private IChannel? _channel;
    public IReadOnlyList<string>? Queues { get; private set; }
    
    public RabbitMqSetupService(
        IRabbitMqChannelPool channelPool,
        ILogger<RabbitMqSetupService> logger,
        IOptionsMonitor<RabbitOptions> rabbitOptions)
    {
        _channelPool = channelPool;
        _logger = logger;
        _rabbitOptions = rabbitOptions.CurrentValue;
    }
    
    public async Task SetupInfrastructureAsync()
    {
        try
        {
            _channel = await _channelPool.GetChannelAsync();

            var library = _rabbitOptions.Library;
            var reports = _rabbitOptions.Reports;
            
            await _channel.ExchangeDeclareAsync(library.ExchangeName , ExchangeType.Topic, durable: true);

            var bindings = typeof(LibraryRabbitConfig)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(QueueBindingConfig))
                .Select(p => (QueueBindingConfig)p.GetValue(library)!);
            
            var queues = new List<string>();
            foreach (var b in bindings)
            {
                await _channel.QueueDeclareAsync(
                    queue: b.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                await _channel.QueueBindAsync(
                    queue: b.QueueName,
                    exchange: library.ExchangeName,
                    routingKey: b.RoutingKey);
                queues.Add(b.QueueName);
            }
            
            await _channel.ExchangeDeclareAsync(reports.Exchange, ExchangeType.Direct, durable: true);

            await _channel.QueueDeclareAsync(
                queue: reports.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            await _channel.QueueBindAsync(
                queue: reports.QueueName,
                exchange: reports.Exchange,
                routingKey: reports.RoutingKey);
            
            queues.Add(reports.QueueName);
            Queues = queues;
            
            _logger.LogInformation("RabbitMQ инфраструктура настроена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка настройки RabbitMQ инфраструктуры");
            throw;
        }
        finally
        {
            if (_channel != null)
                _channelPool.ReturnChannel(_channel);
        }
    }
}