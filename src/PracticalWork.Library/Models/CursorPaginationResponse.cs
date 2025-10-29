namespace PracticalWork.Library.Models;

public class CursorPaginationResponse<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public string NextCursor { get; set; }
    public string PreviousCursor { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}