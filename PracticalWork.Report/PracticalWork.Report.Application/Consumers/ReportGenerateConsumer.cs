using Domain.Events;
using MessageBroker.Rabbit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Events;

namespace PracticalWork.Report.Application.Consumers;

public class ReportGenerateConsumer: RabbitMqConsumer<ReportCreateEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public ReportGenerateConsumer(
        ILogger<RabbitMqConsumer<ReportCreateEvent>> logger, 
        IRabbitMqChannelPool channelPool, 
        IServiceScopeFactory serviceScopeFactory) : base(logger, channelPool)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(ReportCreateEvent? messageObject)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IConsumerService>();
        if (messageObject is { Source: "report-service" })
        {
            await reportService.GenerateReport(
                messageObject.Id, messageObject.PeriodFrom, 
                messageObject.PeriodTo, messageObject.EventTypes.ToArray());
        }
        else
        {
            _logger.LogError("Пришло невалидное сообщение от сервиса отчетов");
        }
    }
}