using Domain.Models;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для модуля библиотеки
/// </summary>
public interface IBookService
{
    /// <summary>
    /// Создание книги
    /// </summary>
    /// <param name="book">книга</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task<Guid> CreateBook(Book book, CancellationToken cancellationToken);

    /// <summary>
    /// Обновление книги
    /// </summary>
    /// <param name="id">идентификатор книги</param>
    /// <param name="book">книга с обновленными параметрами</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task UpdateBook(Guid id, Book book, CancellationToken cancellationToken);

    /// <summary>
    /// Архивирование книги
    /// </summary>
    /// <param name="id">идентификатор книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task<BookArchive> ArchiveBook(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получить страницу с книгами
    /// </summary>
    /// <param name="request">запрос пагинации</param>
    /// <param name="status">фильтр на статус книги</param>
    /// <param name="category">фильтр на категорию книги</param>
    /// <param name="author">фильтр на автора книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>ответ пагинации</returns>
    Task<CursorPaginationResponse<Book>> GetBooksPage(CursorPaginationRequest request, 
        BookStatus? status, BookCategory? category, string author, CancellationToken cancellationToken);

    /// <summary>
    /// Добавить деталей к книге
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="description">описание книги</param>
    /// <param name="coverImageStream">поток изображения обложки книги</param>
    /// <param name="contentType">тип изображения</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task AddBookDetails(Guid bookId, string description, Stream coverImageStream, string contentType,
        CancellationToken cancellationToken);
}