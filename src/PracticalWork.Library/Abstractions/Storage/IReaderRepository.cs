using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных о карточках читателя
/// </summary>
public interface IReaderRepository
{
    /// <summary>
    /// Создать карточку
    /// </summary>
    /// <param name="reader">объект карточки</param>
    /// <returns>идентификатор карточки</returns>
    Task<Guid> CreateReader(Reader reader);
    /// <summary>
    /// Получить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <returns>объект карточки</returns>
    Task<Reader> GetReader(Guid id);
    /// <summary>
    /// Обновить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="reader">объект карточки</param>
    /// <returns></returns>
    Task UpdateReader(Guid id, Reader reader);
    /// <summary>
    /// Получить информацию о карточке вместе с записями выдачи
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <returns>объект карточки</returns>
    Task<Reader> GetReaderWithBorrowBooks(Guid id);
    /// <summary>
    /// Получить выданные читателю книги
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <returns>флаг активности карточки и список выданных книг</returns>
    Task<(bool isActive, IReadOnlyList<BorrowedBook> books)> GetReadersBorrowBooks(Guid id);
    /// <summary>
    /// Проверить существование карточки
    /// </summary>
    /// <param name="phone">телефон читателя</param>
    /// <returns>истина или ложб</returns>
    Task<bool> IsExistReader(string phone);

    Task<int> GetNewReadersCount(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}