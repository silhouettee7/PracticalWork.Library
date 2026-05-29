using Contracts.v1.Abstract;
using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public sealed record BookCursorPaginationResponse(IReadOnlyList<BookResponse> Items, 
    string NextCursor, string PreviousCursor, bool HasNext, bool HasPrevious) : 
    AbstractCursorPaginationResponse<BookResponse>(
        Items, NextCursor, PreviousCursor, HasNext, HasPrevious);