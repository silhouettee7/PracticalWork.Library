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
    /// <summary>
    /// Получить информацию о выданных книгах
    /// </summary>
    /// <param name="from">с какой даты</param>
    /// <param name="to">по какую дату</param>
    /// <param name="dateToleranceMinutesAgo">для планировщика, чтобы повторно не отправить</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>список о выданных книгах</returns>
    Task<List<BorrowedIssuedBookInfoDto>> GetBorrowedIssuedBooksInfo(DateOnly from, DateOnly to,
        DateTime dateToleranceMinutesAgo, CancellationToken cancellationToken);
    /// <summary>
    /// Обновить дату отправления письма
    /// </summary>
    /// <param name="bookBorrowId">идентификатор выдачи</param>
    /// <param name="timestamp">время</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task UpdateLastEmailSentAsync(Guid bookBorrowId, DateTime timestamp, CancellationToken cancellationToken);
    /// <summary>
    /// Получить статистику о выданных книгах
    /// </summary>
    /// <param name="from">с какой даты</param>
    /// <param name="to">по какую дату</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>статистика о выданных книгах</returns>
    Task<BorrowBookStatisticDto> GetBorrowBookStatistic(DateOnly from, DateOnly to,
        CancellationToken cancellationToken);
}