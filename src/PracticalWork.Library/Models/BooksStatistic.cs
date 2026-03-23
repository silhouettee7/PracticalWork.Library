using PracticalWork.Library.Attributes;

namespace PracticalWork.Library.Models;

public class BooksStatistic
{
    [TableColumn("Количество выданных книг")]
    public int BorrowedCount { get; set; } 
    [TableColumn("Количество возвращенных книг")]
    public int ReturnedCount {get; set; }
    [TableColumn("Количество просроченных выдач")]
    public int OverdueCount {get; set; }
    [TableColumn("Количество новых книг")]
    public int AddedBooksCount { get; set; }
    [TableColumn("Количество новых читателей")]
    public int RegisterReadersCount { get; set; }
    public string FileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    
}