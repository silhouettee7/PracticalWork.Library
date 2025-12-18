using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class CursorPaginationExtensions
{
    public static CursorPaginationRequest ToCursorPaginationRequest(this AbstractCursorPaginationRequest request) =>
        new()
        {
            Cursor = request.Cursor,
            Forward = request.Forward,
            PageSize = request.PageSize,
        };

}