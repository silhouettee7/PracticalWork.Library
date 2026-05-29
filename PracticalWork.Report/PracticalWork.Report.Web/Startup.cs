using System;
using System.IO;
using System.Text.Json.Serialization;
using Data.Minio;
using Domain.Exceptions;
using JetBrains.Annotations;
using MessageBroker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using PracticalWork.Report.Application;
using PracticalWork.Report.Controllers;
using PracticalWork.Report.PostgreSql;
using PracticalWork.Report.Web.Configuration;
using Redis;

namespace PracticalWork.Report.Web;

public class Startup
{
    private static string _basePath;
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;

        _basePath = string.IsNullOrWhiteSpace(Configuration["GlobalPrefix"]) ? "" : $"/{Configuration["GlobalPrefix"].Trim('/')}";
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddReportsPostgreSqlStorage(cfg =>
            {
                var connectionString = Configuration
                    .GetSection("App")
                    .GetConnectionString(nameof(ReportsDbContext));
                var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
                    .EnableDynamicJson()
                    .Build();

                cfg.UseNpgsql(npgsqlDataSource);
            }    
        );

        services.AddMvc(opt =>
            {
                opt.Filters.Add<DomainExceptionFilter<AppException>>();
            })
            .AddApi()
            .AddControllersAsServices()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        services.AddSwaggerGen(c =>
        {
            c.UseOneOfForPolymorphism();
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Report.Contracts.xml"));
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Report.Controllers.xml"));
        });

        services.AddBaseDomain();
        services.AddCache(Configuration);
        services.AddMinioFileStorage(Configuration);
        services
            .AddMessageBroker(Configuration)
            .AddProducing();
        
        services.AddSingleton(TimeProvider.System);
    }

    [UsedImplicitly]
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
        ILogger logger, IServiceProvider serviceProvider)
    {
        app.UsePathBase(new PathString(_basePath));

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var descriptions = endpoints.DescribeApiVersions();
                foreach (var description in descriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });
            endpoints.MapControllers();
        });
    }
}
