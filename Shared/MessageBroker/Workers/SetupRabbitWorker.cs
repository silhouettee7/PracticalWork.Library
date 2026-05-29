using MessageBroker.Configuration.Abstractions;
using MessageBroker.Rabbit.Utils;
using Microsoft.Extensions.Hosting;

namespace MessageBroker.Workers;

internal class SetupRabbitWorker: BackgroundService
{
    private readonly RabbitMqSetupService _setupService;
    private readonly IInitializable _initializable;
    
    public SetupRabbitWorker(
        RabbitMqSetupService setupService,
        IInitializable init)
    {
        _setupService = setupService;
        _initializable = init;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _initializable.InitializeAsync();
        await _setupService.SetupInfrastructureAsync();
        _initializable.IsInitialized = true;
    }
}