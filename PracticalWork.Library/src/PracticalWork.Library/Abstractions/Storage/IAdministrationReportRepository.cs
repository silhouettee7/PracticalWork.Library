using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных об отчетах
/// </summary>
public interface IAdministrationReportRepository
{
    Task SaveReportAsync(AdministrationReport report, CancellationToken cancellationToken);
}