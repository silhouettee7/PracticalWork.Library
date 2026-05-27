using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library;
using PracticalWork.Library.Cache.Redis;
using PracticalWork.Library.Data.Minio;
using PracticalWork.Library.MessageBroker;
using PracticalWork.Library.Reports.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMessageBroker(builder.Configuration)
    .AddDefaultConsuming(builder.Configuration);
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

var host = builder.Build();
host.Run();