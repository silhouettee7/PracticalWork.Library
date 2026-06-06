using PracticalWork.Library.Dtos;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных о выдачах книг
/// </summary>
public interface IBorrowRepository
{
    /// <summary>
    /// Создать запись о выдаче
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <param name="bookBorrow">объект выдачи книги</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task CreateBookBorrow(Guid bookId, Guid readerId, BookBorrow bookBorrow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Получить запись о выдаче
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>идентификатор выдачи и объект выдачи</returns>
    Task<(Guid id, BookBorrow bookBorrow)> GetBookBorrow(Guid bookId, Guid readerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Обновить возвращенную книгу
    /// </summary>
    /// <param name="bookBorrowId">идентификатор выдачи</param>
    /// <param name="bookBorrow">объект выдачи</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task ReturnBookBorrow(Guid bookBorrowId, BookBorrow bookBorrow,
        CancellationToken cancellationToken);

    Task<List<BorrowedIssuedBookInfoDto>> GetBorrowedIssuedBooksInfo(DateOnly from, DateOnly to,
        DateTime dateToleranceMinutesAgo, CancellationToken cancellationToken);

    Task UpdateLastEmailSentAsync(Guid bookBorrowId, DateTime timestamp, CancellationToken cancellationToken);
    Task<BorrowBookStatisticDto> GetBorrowBookStatistic(DateOnly from, DateOnly to,
        CancellationToken cancellationToken);
}