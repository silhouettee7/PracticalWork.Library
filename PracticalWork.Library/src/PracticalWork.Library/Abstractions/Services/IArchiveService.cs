namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис архивирования книг
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Заархивировать старые книги
    /// </summary>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task ArchiveOldBooksAsync(CancellationToken cancellationToken);
}