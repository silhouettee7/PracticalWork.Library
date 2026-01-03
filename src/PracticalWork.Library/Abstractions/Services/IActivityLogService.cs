using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IActivityLogService
{
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
}