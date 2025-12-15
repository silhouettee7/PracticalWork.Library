using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Services;
using StackExchange.Redis;

namespace PracticalWork.Library.Cache.Redis;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей для распределенного Cache
    /// </summary>
    public static IServiceCollection AddCache(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var connectionString = configuration["App:Redis:RedisCacheConnection"];
        var prefix = configuration["App:Redis:RedisCachePrefix"];
        serviceCollection.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = prefix;
        });
        serviceCollection.AddScoped<ICacheService, RedisService>();
        return serviceCollection;
    }
}

