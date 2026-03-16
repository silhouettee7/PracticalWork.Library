namespace PracticalWork.Library.Abstractions.Services;

public interface INotificationService
{
    Task NotifyReadersWithIssuedBorrowedBooksAsync();
}