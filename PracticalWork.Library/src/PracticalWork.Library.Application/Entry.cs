using Domain.Abstractions.Services;
using Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Jobs;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Application.Jobs;
using PracticalWork.Library.Application.Services;
using PracticalWork.Library.Models;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Application;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей уровня бизнес-логики
    /// </summary>
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<IReportGenerateService, ReportGenerateService>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAdministrationReportService, AdministrationReportService>();
        services.AddScoped<IEmailMessageTemplateService, EmailMessageTemplateService>();
        
        return services;
    }
    
    /// <summary>
    /// Регистрация зависимостей уровня бизнес-логики (без джоб)
    /// </summary>
    public static IServiceCollection AddBaseDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IReaderService, ReaderService>();
        
        return services;
    }
    
    /// <summary>
    /// Регистрация зависимостей уровня бизнес-логики (только для джоб)
    /// </summary>
    public static IServiceCollection AddBackgroundTasksDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportGenerateService, ReportGenerateService>();
        services.AddScoped<IAdministrationReportService, AdministrationReportService>();
        services.AddScoped<IEmailMessageTemplateService, EmailMessageTemplateService>();
        
        return services;
    }
    
    /// <summary>
    /// Регистрация самих джоб
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.AddSingleton<ILibraryJob, ArchiveJob>();
        services.AddSingleton<ILibraryJob, WeeklyReportJob>();
        services.AddSingleton<ILibraryJob, ReturnRemindersJob>();
        
        return services;
    }

    /// <summary>
    /// Регистрация конфигурации для джоб
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">конфигурация</param>
    /// <returns></returns>
    public static IServiceCollection AddJobsOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SchedulerOptions>(configuration.GetSection("App:Scheduler"));
        services.Configure<EmailMessagesOptions>(configuration.GetSection("App:EmailMessages"));
        services.Configure<BackgroundReportsOptions>(configuration.GetSection("App:BackgroundReports"));
        return services;
    }
    
}