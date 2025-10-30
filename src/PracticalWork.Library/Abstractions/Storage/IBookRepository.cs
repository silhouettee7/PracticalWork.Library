using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IBookRepository
{
    Task<Guid> CreateBook(Book book);
    Task<Book> GetBookById(Guid id);
    Task<(Guid id, Book book)> GetBookByTitle(string title);
    Task UpdateBook(Guid id, Book book);
    Task<IReadOnlyList<Book>> GetBooksPageFilteringByFields(CursorPaginationRequest request, 
        BookStatus status, BookCategory category, string author);

    Task<IReadOnlyList<Book>> GetNonArchivedBooksPageWithIssuanceRecords(CursorPaginationRequest request);
}