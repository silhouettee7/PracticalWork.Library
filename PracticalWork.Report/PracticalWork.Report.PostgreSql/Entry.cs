using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Report.Abstractions.Storage;
using PracticalWork.Report.PostgreSql.Repositories;

namespace PracticalWork.Report.PostgreSql;

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