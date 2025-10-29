namespace PracticalWork.Library.Models;

public class BookArchive
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DateTime ArchivedAt { get; set; }
}