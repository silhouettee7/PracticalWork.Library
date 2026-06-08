using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных об отчетах
/// </summary>
public interface IAdministrationReportRepository
{
    /// <summary>
    /// Сохранить отчет
    /// </summary>
    /// <param name="report">отчет</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task SaveReportAsync(AdministrationReport report, CancellationToken cancellationToken);
}