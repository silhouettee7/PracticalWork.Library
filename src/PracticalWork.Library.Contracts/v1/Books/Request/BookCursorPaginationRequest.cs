using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

public sealed record BookCursorPaginationRequest(string Cursor, int PageSize, bool Forward, 
    BookCategory Category, string Author, BookStatus Status): 
    AbstractCursorPaginationRequest(Cursor, PageSize, Forward);