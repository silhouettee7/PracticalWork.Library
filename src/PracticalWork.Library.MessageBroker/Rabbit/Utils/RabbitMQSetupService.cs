using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Configuration.Models;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Utils;

public class RabbitMQSetupService
{
    private readonly IRabbitMQChannelPool _channelPool;
    private readonly ILogger<RabbitMQSetupService> _logger;
    private readonly IConfiguration _configuration;
    private IChannel? _channel;
    public IReadOnlyList<string>? Queues { get; private set; }
    
    public RabbitMQSetupService(
        IRabbitMQChannelPool channelPool,
        ILogger<RabbitMQSetupService> logger,
        IConfiguration configuration)
    {
        _channelPool = channelPool;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task SetupInfrastructureAsync()
    {
        try
        {
            _channel = await _channelPool.GetChannelAsync();
            var rabbitSection = _configuration.GetSection("App:RabbitMQ");

            var library = rabbitSection.GetSection("Library").Get<LibraryConfig>() ?? new();
            var reports = rabbitSection.GetSection("Reports").Get<ReportsConfig>() ?? new();
            
            await _channel.ExchangeDeclareAsync(library.ExchangeName , ExchangeType.Direct, durable: true);

            var bindings = typeof(LibraryConfig)
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
    }
}