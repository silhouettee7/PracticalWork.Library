using Domain.Abstractions.Services;
using Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Models;
using PracticalWork.Library.Services;

namespace PracticalWork.Library;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей уровня бизнес-логики
    /// </summary>
    public static IServiceCollection AddBaseDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<IReportGenerateService, ReportGenerateService>();
        
        return services;
    }

    public static IServiceCollection AddBackgroundTasksDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportGenerateService, ReportGenerateService>();
        services.AddScoped<IAdministrationReportService, AdministrationReportService>();
        
        return services;
    }
}