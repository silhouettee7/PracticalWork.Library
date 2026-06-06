using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для модуля карточка читателя
/// </summary>
public interface IReaderService
{
    /// <summary>
    /// Создать карточку читателя
    /// </summary>
    /// <param name="reader">объект карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>идентификатор карточки</returns>
    Task<Guid> CreateReader(Reader reader, CancellationToken cancellationToken);

    /// <summary>
    /// Продлить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="date">дата продления</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task ExtendExpiryDate(Guid id, DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// Закрыть карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>флаг существования записей о невозвращенных книгах, список невозвращенных книг</returns>
    Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о выдачах книг
    /// </summary>
    /// <param name="readerId">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>список записей о выдачах</returns>
    Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId, CancellationToken cancellationToken);
}