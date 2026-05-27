using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Extensions;

public static class QueryableExt
{
    public static IQueryable<T> CursorPage<T>(this IQueryable<T> data, CursorPaginationRequest request) where T : EntityBase
    {
        return request.Cursor == null
            ? CursorFirstPage(data, request)
            : CursorOtherPage(data, request);
    }
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