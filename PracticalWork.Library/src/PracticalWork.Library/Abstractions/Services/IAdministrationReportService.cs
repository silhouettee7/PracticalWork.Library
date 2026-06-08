namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для генерации отчетов администраци
/// </summary>
public interface IAdministrationReportService
{
    /// <summary>
    /// Создать отчет и отправить
    /// </summary>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task CreateReportForAdministration(CancellationToken cancellationToken);
}