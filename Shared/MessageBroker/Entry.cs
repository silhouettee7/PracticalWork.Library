using Domain.Abstractions.MessageBroker;
using Domain.Options;
using MessageBroker.Configuration.Abstractions;
using MessageBroker.Rabbit.Abstractions;
using MessageBroker.Rabbit.Publishers;
using MessageBroker.Rabbit.Utils;
using MessageBroker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker;

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
        serviceCollection.AddHostedService<DefaultConsumersBackgroundService>();
        return serviceCollection;
    }
}

