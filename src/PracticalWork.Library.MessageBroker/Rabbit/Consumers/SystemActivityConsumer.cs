using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.MessageBroker.Rabbit.Consumers;

public class SystemActivityConsumer<T>: RabbitMQConsumer<T> where T: BaseEvent
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public SystemActivityConsumer(
        ILogger<RabbitMQConsumer<T>> logger, 
        IRabbitMQChannelPool channelPool,
        IServiceScopeFactory serviceScopeFactory) 
        : base(logger, channelPool)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(T? messageObject)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        if (messageObject is { Source: "library-service" })
        {
            var log = new ActivityLog
            {
                Event = messageObject,
                EventDate = messageObject.OccurredOn,
                EventType = messageObject.EventType
            };
            await reportService.WriteSystemActivityLogs(log);
        }
        else
        {
            _logger.LogError("Пришло невалидное сообщение лога события системы");
        }
    }
}