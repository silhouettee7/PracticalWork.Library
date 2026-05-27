using Microsoft.Extensions.Hosting;
using PracticalWork.Library.MessageBroker.Configuration.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Utils;

namespace PracticalWork.Library.MessageBroker.Workers;

public class SetupRabbitWorker: BackgroundService
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