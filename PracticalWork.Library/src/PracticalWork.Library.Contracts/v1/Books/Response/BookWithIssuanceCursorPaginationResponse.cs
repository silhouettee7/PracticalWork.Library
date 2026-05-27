using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Books.Response;

public sealed record BookWithIssuanceCursorPaginationResponse(IReadOnlyList<BookWithIssuanceRecordsResponse> Items, 
    string NextCursor, string PreviousCursor, bool HasNext, bool HasPrevious) : 
    AbstractCursorPaginationResponse<BookWithIssuanceRecordsResponse>(
        Items, NextCursor, PreviousCursor, HasNext, HasPrevious);