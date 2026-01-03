using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Events;
using PracticalWork.Library.MessageBroker.Configuration.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Consumers;
using PracticalWork.Library.MessageBroker.Rabbit.Publishers;
using PracticalWork.Library.MessageBroker.Rabbit.Utils;
using PracticalWork.Library.MessageBroker.Workers;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.MessageBroker;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей для брокера сообщений
    /// </summary>
    public static IServiceCollection AddMessageBroker(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<RabbitOptions>(
            configuration.GetSection("App:RabbitMQ"));
        serviceCollection.AddSingleton<RabbitMqChannelPool>();
        serviceCollection.AddSingleton<IRabbitMqChannelPool, RabbitMqChannelPool>(
            sp => sp.GetRequiredService<RabbitMqChannelPool>());
        serviceCollection.AddSingleton<IInitializable, RabbitMqChannelPool>(
            sp => sp.GetRequiredService<RabbitMqChannelPool>());
        serviceCollection.AddSingleton<RabbitMqSetupService>();
        serviceCollection.AddHostedService<SetupRabbitWorker>();
        return serviceCollection;
    }
    
    /// <summary>
    /// Регистрация продюсерской части
    /// </summary>
    public static IServiceCollection AddProducing(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();
        return serviceCollection;
    }
    
    /// <summary>
    /// Регистрация консьюмерской части по умолчанию
    /// </summary>
    public static IServiceCollection AddDefaultConsuming(this IServiceCollection serviceCollection, IConfiguration configuration)
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
        serviceCollection.AddHostedService<DefaultConsumersBackgroundService>();
        return serviceCollection;
    }
}

