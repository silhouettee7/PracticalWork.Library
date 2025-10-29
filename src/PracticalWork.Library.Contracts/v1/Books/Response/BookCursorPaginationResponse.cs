using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public sealed record BookCursorPaginationResponse(IReadOnlyList<BookDetailsResponse> Items, 
    string NextCursor, string PreviousCursor, bool HasNext, bool HasPrevious) : 
    AbstractCursorPaginationResponse<BookDetailsResponse>(
        Items, NextCursor, PreviousCursor, HasNext, HasPrevious);