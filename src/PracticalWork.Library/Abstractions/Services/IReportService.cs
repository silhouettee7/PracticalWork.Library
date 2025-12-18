using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис управления отчетами и записями событий системы
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Записать лог
    /// </summary>
    /// <param name="log">запись события системы</param>
    /// <returns>задача</returns>
    Task WriteSystemActivityLogs(ActivityLog log);
    /// <summary>
    /// Прочитать страницу с записями событий системы
    /// </summary>
    /// <param name="request">объект пагинации</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <param name="eventDateFrom">фильтр на дату начала событий системы</param>
    /// <param name="eventDateTo">фильтр на дату окончания событий системы</param>
    /// <returns>объект пагинации с записями</returns>
    Task<CursorPaginationResponse<ActivityLog>> ReadSystemActivityLogs(CursorPaginationRequest request, 
        string[] eventTypes, DateOnly? eventDateFrom, DateOnly? eventDateTo);
    /// <summary>
    /// Создать отчет по событиям системы
    /// </summary>
    /// <param name="eventDateFrom">фильтр на дату начала событий системы</param>
    /// <param name="eventDateTo">фильтр на дату окончания событий системы</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <returns>отчет со статусом "в процессе"</returns>
    Task<Report> CreateReport(DateOnly? eventDateFrom, DateOnly? eventDateTo, string[] eventTypes);
    /// <summary>
    /// Сгенерировать отчет
    /// </summary>
    /// <param name="reportId">идентификатор отчета</param>
    /// <param name="periodFrom">фильтр на дату начала событий системы</param>
    /// <param name="periodTo">фильтр на дату окончания событий системы</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <returns>задача</returns>
    Task GenerateReport(Guid reportId, DateOnly? periodFrom,
        DateOnly? periodTo, string[] eventTypes);
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
}