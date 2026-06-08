namespace Contracts.v1.Abstract;

/// <summary>
/// Базовый класс для запроса пагинации
/// </summary>
/// <param name="Cursor">курсор base64</param>
/// <param name="PageSize">кол-во страниц</param>
/// <param name="Forward">направление</param>
public abstract record AbstractCursorPaginationRequest(string Cursor, 
    int PageSize, bool Forward);