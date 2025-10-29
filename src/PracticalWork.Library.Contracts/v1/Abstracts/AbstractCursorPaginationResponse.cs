namespace PracticalWork.Library.Contracts.v1.Abstracts;

public abstract record AbstractCursorPaginationResponse<T>(IReadOnlyList<T> Items, 
    string NextCursor,string PreviousCursor, bool HasNext, bool HasPrevious);
