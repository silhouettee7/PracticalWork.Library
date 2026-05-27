using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PracticalWork.Report.PostgreSql;

public class ReportsMigrationsRunner
{
    public static async Task ApplyMigrations(ILogger logger, IServiceProvider serviceProvider, string appName)
    {
        var operationId = Guid.NewGuid().ToString()[..4];
        logger.LogInformation($"{appName}:UpdateDatabase:{operationId}: starting...");
        try
        {
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ReportsDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            logger.LogInformation($"{appName}:UpdateDatabase:{operationId}: successfully done");
            await Task.FromResult(true);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{appName}:UpdateDatabase.{operationId}: Migration failed");
            throw;
        }
    }
}