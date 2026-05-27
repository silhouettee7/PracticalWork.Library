using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.MessageBroker.Configuration.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Utils;

namespace PracticalWork.Library.MessageBroker.Workers;

public class DefaultConsumersBackgroundService: BackgroundService
{
    private readonly RabbitMqSetupService _setupService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<IRabbitMqConsumer?> _consumers;
    private readonly IInitializable _initializable;
    
    public DefaultConsumersBackgroundService(
        RabbitMqSetupService setupService,
        IServiceScopeFactory factory,
        IInitializable init)
    {
        _setupService = setupService;
        _consumers = new List<IRabbitMqConsumer?>();
        _scopeFactory = factory;
        _initializable = init;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!_initializable.IsInitialized)
        {
            await Task.Delay(1000, stoppingToken);
        }
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var queues = _setupService.Queues ?? new List<string>();

        foreach (var queue in queues)
        {
            var consumer = sp.GetKeyedService<IRabbitMqConsumer>(queue);
            if (consumer == null) continue;
            await consumer.StartConsuming(queue);
            _consumers.Add(consumer);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers.OfType<IRabbitMqConsumer>())
        {
            await consumer.StopConsuming();
        }
        await base.StopAsync(cancellationToken);
    }
}