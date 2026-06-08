namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для уведомлений
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Уведомить читателей о выдачах
    /// </summary>
    /// <param name="cancellationToken">токен отмены</param>
    /// <returns></returns>
    Task NotifyReadersWithIssuedBorrowedBooksAsync(CancellationToken cancellationToken);
}