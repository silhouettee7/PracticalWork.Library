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
    /// <returns>идентификатор карточки</returns>
    Task<Guid> CreateReader(Reader reader);
    /// <summary>
    /// Продлить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="date">дата продления</param>
    /// <returns></returns>
    Task ExtendExpiryDate(Guid id, DateOnly date);
    /// <summary>
    /// Закрыть карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <returns>флаг существования записей о невозвращенных книгах, список невозвращенных книг</returns>
    Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id);
    /// <summary>
    /// Получить все записи о выдачах книг
    /// </summary>
    /// <param name="readerId">идентификатор карточки</param>
    /// <returns>список записей о выдачах</returns>
    Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId);
}