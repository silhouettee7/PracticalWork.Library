using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Repositories;

namespace PracticalWork.Library.Data.PostgreSql;

public static class Entry
{
    private static readonly Action<DbContextOptionsBuilder> DefaultOptionsAction = (_) => { };

    /// <summary>
    /// Добавления зависимостей для работы с БД
    /// </summary>
    public static IServiceCollection AddPostgreSqlStorage(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction)
    {
        serviceCollection.AddDbContext<AppDbContext>(optionsAction ?? DefaultOptionsAction, optionsLifetime: ServiceLifetime.Singleton);

        serviceCollection.AddScoped<IBookRepository, BookRepository>();

        return serviceCollection;
    }
}