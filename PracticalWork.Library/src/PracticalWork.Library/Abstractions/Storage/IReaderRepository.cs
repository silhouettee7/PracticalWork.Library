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
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>идентификатор карточки</returns>
    Task<Guid> CreateReader(Reader reader, CancellationToken cancellationToken);

    /// <summary>
    /// Получить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>объект карточки</returns>
    Task<Reader> GetReader(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Обновить карточку читателя
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="reader">объект карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task UpdateReader(Guid id, Reader reader, CancellationToken cancellationToken);

    /// <summary>
    /// Получить информацию о карточке вместе с записями выдачи
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>объект карточки</returns>
    Task<Reader> GetReaderWithBorrowBooks(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получить выданные читателю книги
    /// </summary>
    /// <param name="id">идентификатор карточки</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>флаг активности карточки и список выданных книг</returns>
    Task<(bool isActive, IReadOnlyList<BookBorrowWIthDetailInfo> books)> GetReadersBorrowBooks(Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверить существование карточки
    /// </summary>
    /// <param name="phone">телефон читателя</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>истина или ложб</returns>
    Task<bool> IsExistReader(string phone, CancellationToken cancellationToken);
    /// <summary>
    /// Получить кол-во новых читателей
    /// </summary>
    /// <param name="startDate">с какой даты</param>
    /// <param name="endDate">по какую дату</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns>кол-во</returns>
    Task<int> GetNewReadersCount(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}