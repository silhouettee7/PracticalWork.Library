using Data.Minio;
using Hangfire;
using Hangfire.PostgreSql;
using MessageBroker;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library.Application;
using PracticalWork.Library.Data.PostgreSql;
using PracticalWork.Library.Email;
using Redis;

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

services.AddJobsOptions(builder.Configuration);

services.AddHangfire(config => 
    config.UsePostgreSqlStorage(opt => 
        opt.UseNpgsqlConnection(configuration
            .GetSection("App")
            .GetConnectionString("Hangfire"))));

services.AddHangfireServer();

services.AddSingleton(TimeProvider.System);

var host = builder.Build();

host.Run();
