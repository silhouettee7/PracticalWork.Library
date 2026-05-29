using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PracticalWork.Library.Cache.Redis;
using PracticalWork.Library.Controllers;
using PracticalWork.Library.Data.Minio;
using PracticalWork.Library.Data.PostgreSql;
using PracticalWork.Library.Web.Configuration;
using System.Text.Json.Serialization;
using Domain.Exceptions;
using Hangfire;
using Hangfire.PostgreSql;
using PracticalWork.Library.Abstractions.Jobs;
using PracticalWork.Library.BackgroundTasks.Jobs;
using PracticalWork.Library.Controllers.Filters;
using PracticalWork.Library.Email;
using PracticalWork.Library.MessageBroker;
using PracticalWork.Library.Options;
using PracticalWork.Library.Reports.PostgreSql;

namespace PracticalWork.Library.Web;

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
        services.AddPostgreSqlStorage(cfg =>
        {
            var connectionString = Configuration
                .GetSection("App")
                .GetConnectionString(nameof(AppDbContext));
            var npgsqlDataSource = new NpgsqlDataSourceBuilder(connectionString)
                .EnableDynamicJson()
                .Build();

            cfg.UseNpgsql(npgsqlDataSource);
        });

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
                opt.Filters.Add<FileImageValidationFilter>();
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
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Library.Contracts.xml"));
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PracticalWork.Library.Controllers.xml"));
        });

        services.AddBaseDomain();
        services.AddCache(Configuration);
        services.AddMinioFileStorage(Configuration);
        services.AddEmail(Configuration);
        services
            .AddMessageBroker(Configuration)
            .AddProducing();
        
        services.AddHangfire(config => 
            config.UsePostgreSqlStorage(opt => 
                opt.UseNpgsqlConnection(Configuration
                    .GetSection("App")
                    .GetConnectionString("Hangfire"))));
        
        services.AddSingleton<ILibraryJob, ArchiveJob>();
        services.AddSingleton<ILibraryJob, WeeklyReportJob>();
        services.AddSingleton<ILibraryJob, ReturnRemindersJob>();
        
        services.AddSingleton(TimeProvider.System);
        services.AddBackgroundTasksDomain();
        services.Configure<SchedulerOptions>(Configuration.GetSection("App:Scheduler"));
        services.Configure<EmailMessagesOptions>(Configuration.GetSection("App:EmailMessages"));
        services.Configure<BackgroundReportsOptions>(Configuration.GetSection("App:BackgroundReports"));
    }

    [UsedImplicitly]
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
        ILogger logger, IServiceProvider serviceProvider)
    {
        app.UseHangfireDashboard();
        
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

        var jobs = app.ApplicationServices.GetServices<ILibraryJob>();
        foreach (var job in jobs)
        {
            var jobCrone = Configuration[$"App:Jobs:{job.ConfigSectionName}"];
            job.ExecuteCronJob(jobCrone);
        }
    }
}
