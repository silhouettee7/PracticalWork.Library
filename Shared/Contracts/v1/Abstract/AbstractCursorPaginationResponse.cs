namespace Contracts.v1.Abstract;

public abstract record AbstractCursorPaginationResponse<T>(IReadOnlyList<T> Items, 
    string NextCursor,string PreviousCursor, bool HasNext, bool HasPrevious);
