using JetBrains.Annotations;

namespace PracticalWork.Library.Web;

[UsedImplicitly]
public class Program
{
    private static readonly ILogger SystemLogger = CreateLoggerFactory(withConfiguration: false, withConsole: true)
        .CreateLogger<Program>();

    public static async Task Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerOnUnobservedTaskException;

            await RunWebApplication(args);
        }
        catch (Exception exception)
        {
            SystemLogger.LogCritical(exception, "Critical error in Main");
            throw;
        }
    }

    [UsedImplicitly]
    public static async Task RunWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: false);

        var startup = new Startup(builder.Configuration);

        startup.ConfigureServices(builder.Services);

        var app = builder.Build();
        startup.Configure(app, app.Environment, app.Lifetime, app.Logger, app.Services);

        await app.RunAsync();
    }

    private static ILoggerFactory CreateLoggerFactory(bool withConfiguration = true, bool withConsole = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)
            .Build();

        var logger = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.ClearProviders();

                if (withConsole)
                {
                    builder.AddConsole();
                }

                if (withConfiguration)
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                }
            })
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();

        return logger;
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (e.IsTerminating)
        {
            SystemLogger.LogCritical(ex, "Service terminating with fatal exception");
            return;
        }
        SystemLogger.LogError(ex, "Unhandled exception in global handler");
    }

    private static void OnTaskSchedulerOnUnobservedTaskException(object sender,
        UnobservedTaskExceptionEventArgs eventArgs)
    {
        eventArgs.SetObserved();
        eventArgs.Exception.Flatten().Handle(ex =>
        {
            SystemLogger.LogError(ex, "Unhandled exception in Task Scheduler handler");
            return true;
        });
    }
}