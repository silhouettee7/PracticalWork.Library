using System.Text;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Services;

public class CursorPaginationService<TModel>: ICursorPaginationService<TModel>  where TModel : IModel
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
            response.NextCursor = response.HasNext ? EncodeCursor(additionalElem!.Id) : null;
        }
        else
        {
            if (request.Cursor != null)
            {
                response.HasNext = true;
                response.NextCursor = request.Cursor;
            }
            response.HasPrevious = additionalElem is not null;
            response.PreviousCursor = response.HasPrevious ? EncodeCursor(additionalElem!.Id) : null;
        }

        return response;
    }
    private string EncodeCursor(Guid cursor)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(cursor.ToString()));
    }

}