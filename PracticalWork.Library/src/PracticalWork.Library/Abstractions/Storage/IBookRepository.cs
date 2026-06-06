using Domain.Models;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных о книгах
/// </summary>
public interface IBookRepository
{
    /// <summary>
    /// Создать книгу
    /// </summary>
    /// <param name="book">Модель книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>Идентификатор созданной книги</returns>
    Task<Guid> CreateBook(Book book, CancellationToken cancellationToken);

    /// <summary>
    /// Получить книгу по идентификатору
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task<Book> GetBookById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получить книгу по названию
    /// </summary>
    /// <param name="title">название книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>идентификатор книги</returns>
    Task<(Guid id, Book book)> GetBookByTitle(string title, CancellationToken cancellationToken);

    /// <summary>
    /// Отредактировать книгу
    /// </summary>
    /// <param name="book">Модель для редактирования книги</param>
    /// <param name="id">Идентификатор книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>-</returns>
    Task UpdateBook(Guid id, Book book, CancellationToken cancellationToken);

    /// <summary>
    /// Получить страницу книг с фильтрами
    /// </summary>
    /// <param name="request">запрос курсорной пагинации</param>
    /// <param name="status">фильтр статуса книги</param>
    /// <param name="category">фильтр категории книги</param>
    /// <param name="author">фильтр автора книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>список книг</returns>
    Task<IReadOnlyList<Book>> GetBooksPageFilteringByFields(CursorPaginationRequest request, 
        BookStatus? status, BookCategory? category, string author, CancellationToken cancellationToken);

    /// <summary>
    /// Получить не архивные книги с записями о выдаче
    /// </summary>
    /// <param name="request">объект пагинации</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>список книг</returns>
    Task<IReadOnlyList<Book>> GetNonArchivedBooksPageWithIssuanceRecords(CursorPaginationRequest request,
        CancellationToken cancellationToken);
    
    Task<List<AvailableOldBookDto>> GetAvailableOldBooksPage(DateOnly borrowDateTo,
        CursorPaginationRequest request, CancellationToken cancellationToken);
    Task<int> GetAddedBooksCount(DateTime startDate, DateTime endDate, 
        CancellationToken cancellationToken);
}