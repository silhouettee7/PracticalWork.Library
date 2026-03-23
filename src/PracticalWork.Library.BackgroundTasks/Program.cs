using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library;
using PracticalWork.Library.Cache.Redis;
using PracticalWork.Library.Data.Minio;
using PracticalWork.Library.Data.PostgreSql;
using PracticalWork.Library.Email;
using PracticalWork.Library.MessageBroker;
using PracticalWork.Library.Options;
using PracticalWork.Library.Reports.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;

services
    .AddBackgroundTasksDomain()
    .AddMinioFileStorage(configuration)
    .AddCache(configuration)
    .AddEmail(configuration)
    .AddMessageBroker(configuration)
    .AddProducing();

services.AddPostgreSqlStorage(cfg =>
{
    var connectionString = configuration
        .GetSection("App")
        .GetConnectionString(nameof(AppDbContext));
    var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
        .EnableDynamicJson()
        .Build();

    cfg.UseNpgsql(npgsqlDataSource);
});

services.AddReportsPostgreSqlStorage(cfg =>
    {
        var connectionString = configuration
            .GetSection("App")
            .GetConnectionString(nameof(ReportsDbContext));
        var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();

        cfg.UseNpgsql(npgsqlDataSource);
    }    
);

services.Configure<SchedulerOptions>(configuration.GetSection("App:Scheduler"));
services.Configure<EmailMessagesOptions>(configuration.GetSection("App:EmailMessages"));
services.Configure<BackgroundReportsOptions>(configuration.GetSection("App:BackgroundReports"));

services.AddHangfire(config => 
    config.UsePostgreSqlStorage(opt => 
        opt.UseNpgsqlConnection(configuration
            .GetSection("App")
            .GetConnectionString("Hangfire"))));

services.AddHangfireServer();

services.AddSingleton(TimeProvider.System);

var host = builder.Build();

host.Run();
