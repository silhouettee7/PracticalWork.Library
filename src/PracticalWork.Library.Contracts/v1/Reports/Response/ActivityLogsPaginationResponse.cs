using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Reports.Response;

/// <summary>
/// Объект пагинации с записями о событиях системы
/// </summary>
/// <param name="Items">список событий</param>
/// <param name="NextCursor">следующий курсор</param>
/// <param name="PreviousCursor">предыдущий курсор</param>
/// <param name="HasNext">флаг, есть ли следующая запись</param>
/// <param name="HasPrevious">флаг, есть ли предыдущая запись</param>
public record ActivityLogsPaginationResponse(IReadOnlyList<ActivityLogResponse> Items, 
    string NextCursor, string PreviousCursor, bool HasNext, bool HasPrevious) : 
    AbstractCursorPaginationResponse<ActivityLogResponse>(
        Items, NextCursor, PreviousCursor, HasNext, HasPrevious);