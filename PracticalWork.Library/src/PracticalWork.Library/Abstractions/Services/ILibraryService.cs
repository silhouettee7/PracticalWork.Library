using PracticalWork.Library.Dtos;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для модуля работы библиотека
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Выдать книгу
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <returns></returns>
    Task BorrowBook(Guid bookId, Guid readerId);
    /// <summary>
    /// Вернуть книгу
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <returns></returns>
    Task ReturnBook(Guid bookId, Guid readerId);
    /// <summary>
    /// Получить детали книги по идентификатору
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <returns>идентификатор книги и объект книги</returns>
    Task<BookDetailsDto> GetBookDetails(Guid bookId);
    /// <summary>
    /// Получить детали книги по названию
    /// </summary>
    /// <param name="title">название книги</param>
    /// <returns>идентификатор книги и объект книги</returns>
    Task<BookDetailsDto> GetBookDetails(string title);
    /// <summary>
    /// Получить не архивные книги постранично
    /// </summary>
    /// <param name="request">запрос пагинации</param>
    /// <returns>ответ пагинации</returns>
    Task<CursorPaginationResponse<Book>> GetNonArchivedBooksPage(CursorPaginationRequest request);
}