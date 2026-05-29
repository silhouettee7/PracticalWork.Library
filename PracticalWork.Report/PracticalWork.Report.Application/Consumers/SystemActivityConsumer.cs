using Domain.Events;
using MessageBroker.Rabbit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Application.Consumers;

public class SystemActivityConsumer<T>: RabbitMqConsumer<T> where T: BaseEvent
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public SystemActivityConsumer(
        ILogger<RabbitMqConsumer<T>> logger, 
        IRabbitMqChannelPool channelPool,
        IServiceScopeFactory serviceScopeFactory) 
        : base(logger, channelPool)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(T? messageObject)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var activityLogService = scope.ServiceProvider.GetRequiredService<IConsumerService>();
        if (messageObject is { Source: "library-service" })
        {
            var log = new ActivityLog
            {
                Event = messageObject,
                EventDate = messageObject.OccurredOn,
                EventType = messageObject.EventType
            };
            await activityLogService.WriteSystemActivityLogs(log);
        }
        else
        {
            _logger.LogError("Пришло невалидное сообщение лога события системы");
        }
    }
}