using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IBorrowRepository
{
    /// <summary>
    /// Создать запись о выдаче
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <param name="bookBorrow">объект выдачи книги</param>
    /// <returns></returns>
    Task CreateBookBorrow(Guid bookId, Guid readerId, BookBorrow bookBorrow);
    /// <summary>
    /// Получить запись о выдаче
    /// </summary>
    /// <param name="bookId">идентификатор книги</param>
    /// <param name="readerId">идентификатор карточки</param>
    /// <returns>идентификатор выдачи и объект выдачи</returns>
    Task<(Guid id, BookBorrow bookBorrow)> GetBookBorrow(Guid bookId, Guid readerId);
    /// <summary>
    /// Обновить возвращенную книгу
    /// </summary>
    /// <param name="bookBorrowId">идентификатор выдачи</param>
    /// <param name="bookBorrow">объект выдачи</param>
    /// <returns></returns>
    Task ReturnBookBorrow(Guid bookBorrowId, BookBorrow bookBorrow);
}