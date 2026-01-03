using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IConsumerService
{
    /// <summary>
    /// Записать лог
    /// </summary>
    /// <param name="log">запись события системы</param>
    /// <returns>задача</returns>
    Task WriteSystemActivityLogs(ActivityLog log);
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
}