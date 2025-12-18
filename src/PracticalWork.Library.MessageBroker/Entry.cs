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
using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей для брокера сообщений
    /// </summary>
    public static IServiceCollection AddMessageBroker(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var librarySection = configuration.GetSection("App:RabbitMQ:Library");
        var bookCreateQueue = librarySection["BookCreate:QueueName"];
        var bookArchiveQueue = librarySection["BookArchive:QueueName"];
        var bookBorrowQueue = librarySection["BookBorrow:QueueName"];
        var bookReturnQueue = librarySection["BookReturn:QueueName"];
        var readerCreateQueue = librarySection["ReaderCreate:QueueName"];
        var readerCloseQueue = librarySection["ReaderClose:QueueName"];
        var reportsSection = configuration.GetSection("App:RabbitMQ:Reports");
        var reportQueue = reportsSection["QueueName"];
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<BookCreatedEvent>>(bookCreateQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<BookArchivedEvent>>(bookArchiveQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<BookReturnedEvent>>(bookReturnQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<BookBorrowedEvent>>(bookBorrowQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<ReaderCreatedEvent>>(readerCreateQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, SystemActivityConsumer<ReaderClosedEvent>>(readerCloseQueue);
        serviceCollection
            .AddKeyedSingleton<IRabbitMQConsumer, ReportGenerateConsumer>(reportQueue);
        serviceCollection.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
        serviceCollection.AddSingleton<RabbitMQChannelPool>();
        serviceCollection.AddSingleton<IRabbitMQChannelPool, RabbitMQChannelPool>(
            sp => sp.GetRequiredService<RabbitMQChannelPool>());
        serviceCollection.AddSingleton<IInitializable, RabbitMQChannelPool>(
            sp => sp.GetRequiredService<RabbitMQChannelPool>());
        serviceCollection.AddSingleton<RabbitMQSetupService>();
        serviceCollection.AddHostedService<ConsumersBackgroundService>();
        return serviceCollection;
    }
}

