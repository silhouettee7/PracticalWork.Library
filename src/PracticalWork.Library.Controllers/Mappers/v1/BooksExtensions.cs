using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;
using BookIssueStatus = PracticalWork.Library.Contracts.v1.Enums.BookIssueStatus;
using BookStatus = PracticalWork.Library.Contracts.v1.Enums.BookStatus;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class BooksExtensions
{
    public static Book ToBook(this CreateBookRequest request) =>
        new()
        {
            Authors = request.Authors,
            Title = request.Title,
            Description = request.Description,
            Year = request.Year,
            Category = (BookCategory)request.Category
        };

    public static Book ToBook(this UpdateBookRequest request) =>
        new()
        {
            Authors = request.Authors,
            Description = request.Description,
            Title = request.Title,
            Year = request.Year,
        };

    public static ArchiveBookResponse ToArchiveBookResponse(this BookArchive book) =>
        new(book.Id, book.Title, book.ArchivedAt);
    
    public static BookResponse ToBookDetailsResponse(this Book book) =>
        new BookResponse(book.Title, (Contracts.v1.Enums.BookCategory)book.Category, book.Authors, book.Description, book.Year, (BookStatus)book.Status, book.IsArchived);
    public static CursorPaginationRequest ToCursorPaginationRequest(this BookCursorPaginationRequest request) =>
        new()
        {
            Cursor = request.Cursor,
            Forward = request.Forward,
            PageSize = request.PageSize,
        };
    
    public static BookCursorPaginationResponse ToBookCursorPaginationResponse(this CursorPaginationResponse<Book> response) =>
        new (
            response.Items
                .Select(b => b.ToBookDetailsResponse())
                .ToList(), 
            response.NextCursor, response.PreviousCursor, response.HasNext, response.HasPrevious);

    public static BorrowedBookResponse ToBorrowedBookResponse(this BorrowedBook book) =>
        new (
            book.Title, 
            (Contracts.v1.Enums.BookCategory)book.Category, 
            book.Authors,
            book.Description, 
            book.Year, 
            (BookIssueStatus)book.Status, 
            book.DueDate, 
            book.ReturnDate, 
            book.BorrowDate);
}