using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Models;

/// <summary>
/// Выдача книги
/// </summary>
public class BookBorrow
{
    /// <summary>Дата выдачи книги</summary>
    public DateOnly BorrowDate { get; set; }

    /// <summary>Срок возврата книги</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>Фактическая дата возврата книги</summary>
    public DateOnly ReturnDate { get; set; }

    /// <summary>Статус книги в библиотеке</summary>
    public BookIssueStatus Status { get; set; }
    /// <summary>
    /// Объект книги
    /// </summary>
    public Book Book { get; set; }
    /// <summary>
    ///  Создает новый объект выдачи книги
    /// </summary>
    /// <returns>объект выдачи книги</returns>
    public static BookBorrow CreateBookBorrow(TimeProvider timeProvider)
    {
        var bookBorrow = new BookBorrow
        {
            BorrowDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime),
            Status = BookIssueStatus.Issued
        };
        bookBorrow.DueDate = bookBorrow.BorrowDate.AddDays(30);
        return bookBorrow;
    }
    /// <summary>
    /// Возвращает книгу в библиотеку
    /// </summary>
    public void ReturnBookBorrow(TimeProvider timeProvider)
    {
        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        Status = currentDate <= DueDate ? BookIssueStatus.Returned : BookIssueStatus.Overdue;
        ReturnDate = currentDate;
        Book.Status = BookStatus.Available;
    }
}