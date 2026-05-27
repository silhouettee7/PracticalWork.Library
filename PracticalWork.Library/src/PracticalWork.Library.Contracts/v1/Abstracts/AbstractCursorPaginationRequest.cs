namespace PracticalWork.Library.Contracts.v1.Abstracts;

public abstract record AbstractCursorPaginationRequest(string Cursor, 
    int PageSize, bool Forward);