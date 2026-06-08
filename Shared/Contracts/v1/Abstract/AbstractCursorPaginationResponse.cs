namespace Contracts.v1.Abstract;

/// <summary>
/// Базовый класс для ответа пагинации
/// </summary>
/// <param name="Items">записи</param>
/// <param name="NextCursor">следующая страница</param>
/// <param name="PreviousCursor">предыдущая страница</param>
/// <param name="HasNext">есть ли следующая страница</param>
/// <param name="HasPrevious">есть ли предыдущая страница</param>
/// <typeparam name="T"></typeparam>
public abstract record AbstractCursorPaginationResponse<T>(IReadOnlyList<T> Items, 
    string NextCursor,string PreviousCursor, bool HasNext, bool HasPrevious);
