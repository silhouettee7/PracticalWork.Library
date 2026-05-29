using System.Text;
using Domain.Abstractions.Services;
using Domain.Models;

namespace Domain.Services;


public class CursorPaginationService<TModel>: ICursorPaginationService<TModel>  where TModel : ICursor
{
    public CursorPaginationResponse<TModel> ToCursorPageResponse(IReadOnlyList<TModel> page, CursorPaginationRequest request)
    {
        var additionalElem = page
            .Skip(request.PageSize)
            .FirstOrDefault();
        var response = new CursorPaginationResponse<TModel>
        {
            Items = additionalElem is null ? page: page
                .Take(request.PageSize)
                .ToList()
        };
        
        if (request.Forward)
        {
            if (request.Cursor != null)
            {
                response.HasPrevious = true;
                response.PreviousCursor = request.Cursor;
            }
            response.HasNext = additionalElem is not null;
            response.NextCursor = response.HasNext ? EncodeCursor(additionalElem!.Cursor) : null;
        }
        else
        {
            if (request.Cursor != null)
            {
                response.HasNext = true;
                response.NextCursor = request.Cursor;
            }
            response.HasPrevious = additionalElem is not null;
            response.PreviousCursor = response.HasPrevious ? EncodeCursor(additionalElem!.Cursor) : null;
        }

        return response;
    }
    private string EncodeCursor(Cursor cursor)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(cursor.Id.ToString()));
    }

}