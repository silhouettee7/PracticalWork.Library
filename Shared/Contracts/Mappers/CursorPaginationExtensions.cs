using Contracts.v1.Abstract;
using Domain.Models;

namespace Contracts.Mappers;

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