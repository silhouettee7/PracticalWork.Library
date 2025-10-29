using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

public interface IBookService
{
    /// <summary>
    /// Создание книги
    /// </summary>
    Task<Guid> CreateBook(Book book);
    /// <summary>
    /// Обновление книги
    /// </summary>
    /// <param name="id">идентификатор книги</param>
    /// <param name="book">книга с обновленными параметрами</param>
    /// <returns></returns>
    Task UpdateBook(Guid id, Book book);
    /// <summary>
    /// Архивирование книги
    /// </summary>
    /// <param name="id">идентификатор книги</param>
    /// <returns></returns>
    Task<BookArchive> ArchiveBook(Guid id);
    Task<CursorPaginationResponse<Book>> GetBooksPage(CursorPaginationRequest request, 
        BookStatus status, BookCategory category, string author);
    Task AddBookDetails(Guid bookId, string description, Stream coverImageStream, string contentType);
}