namespace Contracts.v1.Abstract;

public abstract record AbstractCursorPaginationRequest(string Cursor, 
    int PageSize, bool Forward);