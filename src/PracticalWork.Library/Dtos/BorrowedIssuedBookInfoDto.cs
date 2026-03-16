namespace PracticalWork.Library.Dtos;

public class BorrowedIssuedBookInfoDto
{
    public Guid Id { get; set; }
    public string ReaderFullName { get; set; }
    public string BookTitle { get; set; }
    public IReadOnlyList<string> Authors { get; set; }
    public DateOnly DueDate { get; set; }
}