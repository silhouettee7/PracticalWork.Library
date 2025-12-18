using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Events;
using PracticalWork.Library.Models;
using PracticalWork.Library.Services;

namespace PracticalWork.Library;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей уровня бизнес-логики
    /// </summary>
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICursorPaginationService<Book>, CursorPaginationService<Book>>();
        services.AddScoped<ICursorPaginationService<ActivityLog>, CursorPaginationService<ActivityLog>>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportGenerateService, ReportGenerateService>();
        return services;
    }
}