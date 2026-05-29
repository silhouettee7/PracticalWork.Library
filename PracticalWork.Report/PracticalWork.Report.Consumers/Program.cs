using Data.Minio;
using MessageBroker;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Report.Application;
using PracticalWork.Report.PostgreSql;
using Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMessageBroker(builder.Configuration)
    .AddDefaultConsuming(builder.Configuration)
    .AddConsumers(builder.Configuration);

builder.Services.AddConsumerDomain();
builder.Services.AddReportsPostgreSqlStorage(cfg =>
    {
        var connectionString = builder.Configuration
            .GetSection("App")
            .GetConnectionString(nameof(ReportsDbContext));
        var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
            .EnableDynamicJson()
            .Build();

        cfg.UseNpgsql(npgsqlDataSource);
    }    
);

builder.Services.AddMinioFileStorage(builder.Configuration);
builder.Services.AddCache(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);

var host = builder.Build();
host.Run();