using Domain.Abstractions.MessageBroker;
using Domain.Abstractions.Services;
using Domain.Events;
using Domain.Options;
using Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Report.Abstractions.Services;
using PracticalWork.Report.Application.Consumers;
using PracticalWork.Report.Application.Services;
using PracticalWork.Report.Models;

namespace PracticalWork.Report.Application;

public static class Entry
{
    /// <summary>
    /// Регистрация консьюмерской части по умолчанию
    /// </summary>
    public static IServiceCollection AddConsumers(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var options = configuration.GetSection("App:RabbitMQ").Get<RabbitOptions>() ?? new RabbitOptions();
        var librarySection = options.Library;
        var bookCreateQueue = librarySection.BookCreate.QueueName;
        var bookArchiveQueue = librarySection.BookArchive.QueueName;
        var bookBorrowQueue = librarySection.BookBorrow.QueueName;
        var bookReturnQueue = librarySection.BookReturn.QueueName;
        var readerCreateQueue = librarySection.ReaderCreate.QueueName;
        var readerCloseQueue = librarySection.ReaderClose.QueueName;
        var reportQueue = options.Reports.QueueName;
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<BookCreatedEvent>>(bookCreateQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<BookArchivedEvent>>(bookArchiveQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<BookReturnedEvent>>(bookReturnQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<BookBorrowedEvent>>(bookBorrowQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<ReaderCreatedEvent>>(readerCreateQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, SystemActivityConsumer<ReaderClosedEvent>>(readerCloseQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMqConsumer, ReportGenerateConsumer>(reportQueue);
        return serviceCollection;
    }

    public static IServiceCollection AddBaseDomain(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICursorPaginationService<ActivityLog>, CursorPaginationService<ActivityLog>>();
        serviceCollection.AddScoped<IReportService, ReportService>();
        serviceCollection.AddScoped<IActivityLogService, ActivityLogService>();

        return serviceCollection;
    }
    
    public static IServiceCollection AddConsumerDomain(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IConsumerService, ConsumerService>();
        serviceCollection.AddScoped<IActivityReportGenerateService, ActivityReportGenerateService>();
        
        return serviceCollection;
    }

}

