using Domain.Abstractions.Storage;
using Domain.Models;

namespace Domain.Extensions;

public static class QueryableExt
{
    /// <summary>
    /// Курсорная пагинация
    /// </summary>
    /// <param name="data">запрос в источник</param>
    /// <param name="request">объект пагинации</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>запрос в источник с пагинацией</returns>
    public static IQueryable<T> CursorPage<T>(this IQueryable<T> data, CursorPaginationRequest request) where T : EntityBase
    {
        return request.Cursor == null
            ? CursorFirstPage(data, request)
            : CursorOtherPage(data, request);
    }
    
    /// <summary>
    /// Курсорная пагинация для первой страницы
    /// </summary>
    /// <param name="data">источник данных</param>
    /// <param name="request">объект пагинации</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>запрос в источник с пагинацией</returns>
    public static IQueryable<T> CursorFirstPage<T>(this IQueryable<T> data, 
        CursorPaginationRequest request) where T : EntityBase
    {
        if (request.Forward)
        {
            return data
                .OrderBy(x => x.Id)
                .Take(request.PageSize + 1);
        }
        return data
            .OrderByDescending(x => x.Id)
            .Take(request.PageSize + 1);
    }
    
    /// <summary>
    /// Курсорная пагинация кроме первой страницы
    /// </summary>
    /// <param name="data">источник данных</param>
    /// <param name="request">объект пагинации</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>запрос в источник с пагинацией</returns>
    public static IQueryable<T> CursorOtherPage<T>(this IQueryable<T> data, 
        CursorPaginationRequest request) where T : EntityBase
    {
        data = data.OrderBy(x => x.Id);
        var id = request.DecodeCursor().Id;
        if (request.Forward)
        {
            data = data
                .Where(x => x.Id.CompareTo(id) >= 0);
        }
        else
        {
            data = data
                .Where(x => x.Id.CompareTo(id) <= 0)
                .OrderByDescending(x => x.Id);
        }
        return data
            .Take(request.PageSize + 1);
    }
}