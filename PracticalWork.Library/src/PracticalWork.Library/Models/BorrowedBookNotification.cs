namespace PracticalWork.Library.Models;

public class BorrowedBookNotification
{
    public string ReaderFullName { get; set; }
    public string BookTitle { get; set; }
    public IReadOnlyCollection<string> Authors { get; set; }
    public DateOnly ReturnDate { get; set; }
    public byte DaysCountBeforeReturn { get; set; }
    public string LibraryAddress { get; set; } = "Kazan";
    public string LibraryPhoneNumber { get; set; } = "+71234567890";
    public string OpeningHours { get; set; } = "8:00-20:00";
}