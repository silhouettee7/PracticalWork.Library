namespace PracticalWork.Library.Contracts.v1.Abstracts;

public abstract record class AbstractCursorPaginationRequest(string Cursor, 
    int PageSize, bool Forward);