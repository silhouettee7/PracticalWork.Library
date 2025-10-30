using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Models;

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

    public Book Book { get; set; }

    public static BookBorrow CreateBookBorrow()
    {
        var bookBorrow = new BookBorrow
        {
            BorrowDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = BookIssueStatus.Issued
        };
        bookBorrow.DueDate = bookBorrow.BorrowDate.AddDays(30);
        
        return bookBorrow;
    }

    public void ReturnBookBorrow()
    {
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        Status = currentDate <= DueDate ? BookIssueStatus.Returned : BookIssueStatus.Overdue;
        ReturnDate = currentDate;
        Book.Status = BookStatus.Available;
    }
}