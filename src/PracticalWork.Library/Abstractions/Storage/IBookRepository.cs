using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IBookRepository
{
    Task<Guid> CreateBook(Book book);
    Task<Book> GetBook(Guid id);
    Task UpdateBook(Guid id, Book book);
    Task<IReadOnlyList<Book>> GetBooksPageFilteringByFields(CursorPaginationRequest request, 
        BookStatus status, BookCategory category, string author);
}