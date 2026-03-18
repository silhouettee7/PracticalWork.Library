using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис управления отчетами и записями событий системы
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Создать отчет по событиям системы
    /// </summary>
    /// <param name="eventDateFrom">фильтр на дату начала событий системы</param>
    /// <param name="eventDateTo">фильтр на дату окончания событий системы</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <returns>отчет со статусом "в процессе"</returns>
    Task<Report> CreateReport(DateOnly? eventDateFrom, DateOnly? eventDateTo, string[] eventTypes);
    /// <summary>
    /// Получить список готовых отчетов
    /// </summary>
    /// <returns>список готовых отчетов</returns>
    Task<IReadOnlyList<Report>> GetListOfReadyReports();
    /// <summary>
    /// Получить ссылку на отчет
    /// </summary>
    /// <param name="reportName">название файла отчета</param>
    /// <returns>url файла</returns>
    Task<string> GetReportUrl(string reportName);

    Task CreateReportForAdministration();
}