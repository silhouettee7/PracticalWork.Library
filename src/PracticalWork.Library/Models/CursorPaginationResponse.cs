namespace PracticalWork.Library.Models;

/// <summary>
/// Ответ пагинации
/// </summary>
/// <typeparam name="T">тип объектов пагинации</typeparam>
public class CursorPaginationResponse<T>
{
    /// <summary>
    /// список элементов в рамках страницы
    /// </summary>
    public required IReadOnlyList<T> Items { get; set; }
    /// <summary>
    ///  курсор следующей записи
    /// </summary>
    public string NextCursor { get; set; }
    /// <summary>
    /// курсор предыдущей записи
    /// </summary>
    public string PreviousCursor { get; set; }
    /// <summary>
    /// флаг, есть ли следующая запись
    /// </summary>
    public bool HasNext { get; set; }
    /// <summary>
    /// флаг, есть ли предыдущая запись
    /// </summary>
    public bool HasPrevious { get; set; }
}