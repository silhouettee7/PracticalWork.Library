using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных записей событий системы
/// </summary>
public interface IActivityLogRepository
{
    /// <summary>
    /// Добавить запись события
    /// </summary>
    /// <param name="activityLog">объект события</param>
    /// <returns>задача</returns>
    Task AddLogAsync(ActivityLog activityLog);
    /// <summary>
    /// Получить страницу записей событий
    /// </summary>
    /// <param name="request">объект пагинации</param>
    /// <param name="periodFrom">фильтр на начало даты</param>
    /// <param name="periodTo">фильтр на окончание даты</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <returns>список записей</returns>
    Task<IReadOnlyList<ActivityLog>> GetLogsPageAsync(CursorPaginationRequest request,
        DateOnly? periodFrom, DateOnly? periodTo, string[] eventTypes);
    
    /// <summary>
    /// Получить записи событий
    /// </summary>
    /// <param name="periodFrom">фильтр на начало даты</param>
    /// <param name="periodTo">фильтр на окончание даты</param>
    /// <param name="eventTypes">фильтр на типы событий</param>
    /// <returns>список записей</returns>
    Task<IReadOnlyList<ActivityLog>> GetLogsAsync(
        DateOnly? periodFrom, DateOnly? periodTo, string[] eventTypes);
}