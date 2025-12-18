using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Reports.PostgreSql.Repositories;

namespace PracticalWork.Library.Reports.PostgreSql;

public static class Entry
{
    /// <summary>
    /// Добавления зависимостей для работы с БД
    /// </summary>
    public static IServiceCollection AddReportsPostgreSqlStorage(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction)
    {
        serviceCollection.AddDbContext<ReportsDbContext>(optionsAction);
        serviceCollection.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        serviceCollection.AddScoped<IReportRepository, ReportRepository>();
        return serviceCollection;
    }
}