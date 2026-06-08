using Contracts.v1.Abstract;
using PracticalWork.Library.Contracts.v1.Abstracts;
using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Request;

/// <summary>
/// Запрос пагинации на книгу
/// </summary>
/// <param name="Cursor">курсор</param>
/// <param name="PageSize">кол-во страниц</param>
/// <param name="Forward">напаравление</param>
/// <param name="Category">категория книги</param>
/// <param name="Author">автор</param>
/// <param name="Status">статус</param>
public sealed record BookCursorPaginationRequest(string Cursor, int PageSize, bool Forward, 
    BookCategory? Category, string Author, BookStatus? Status): 
    AbstractCursorPaginationRequest(Cursor, PageSize, Forward);