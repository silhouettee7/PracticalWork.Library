namespace PracticalWork.Library.Models;

public class BorrowedBookNotification
{
    public string ReaderFullName { get; set; }
    public string BookTitle { get; set; }
    public IReadOnlyCollection<string> Authors { get; set; }
    public DateOnly ReturnDate { get; set; }
    public byte DaysCountBeforeReturn { get; set; }
}