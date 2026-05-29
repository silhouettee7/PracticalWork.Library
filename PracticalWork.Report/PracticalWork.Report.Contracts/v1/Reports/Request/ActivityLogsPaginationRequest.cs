using Contracts.v1.Abstract;

namespace PracticalWork.Report.Contracts.v1.Reports.Request;

/// <summary>
/// Объект пагинации с фильтрами для получения данных о записях событий системы
/// </summary>
/// <param name="Cursor">закодированная строка-курсор</param>
/// <param name="PageSize">размер страницы</param>
/// <param name="Forward">направление пагинации</param>
/// <param name="PeriodFrom">фильтр на дату начала</param>
/// <param name="PeriodTo">фильтр на дату окончания</param>
/// <param name="EventTypes">фильтр на типы событий</param>
public record ActivityLogsPaginationRequest(string Cursor, int PageSize, bool Forward, 
    DateOnly? PeriodFrom, DateOnly? PeriodTo, IReadOnlyList<string> EventTypes)
    :AbstractCursorPaginationRequest(Cursor, PageSize, Forward);