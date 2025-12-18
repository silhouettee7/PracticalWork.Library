using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.MessageBroker.Configuration.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Utils;

namespace PracticalWork.Library.MessageBroker.Workers;

public class ConsumersBackgroundService: BackgroundService
{
    private readonly RabbitMQSetupService _setupService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<IRabbitMQConsumer?> _consumers;
    private readonly IInitializable? _initializable;
    
    public ConsumersBackgroundService(
        RabbitMQSetupService setupService,
        IServiceScopeFactory factory,
        IInitializable init)
    {
        _setupService = setupService;
        _consumers = new List<IRabbitMQConsumer?>();
        _scopeFactory = factory;
        _initializable = init;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_initializable != null)
        {
            await _initializable.InitializeAsync();
        }
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        await _setupService.SetupInfrastructureAsync();

        var queues = _setupService.Queues!;

        foreach (var queue in queues)
        {
            var consumer = sp.GetKeyedService<IRabbitMQConsumer>(queue);
            if (consumer == null) continue;
            await consumer.StartConsuming(queue);
            _consumers.Add(consumer);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers.OfType<IRabbitMQConsumer>())
        {
            await consumer.StopConsuming();
        }
        await base.StopAsync(cancellationToken);
    }
}