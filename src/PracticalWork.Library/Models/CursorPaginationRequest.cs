using System.Text;

namespace PracticalWork.Library.Models;

public class CursorPaginationRequest
{
    public string Cursor { get; set; }
    public required int PageSize { get; set; }
    public required bool Forward { get; set; }
    
    public Cursor DecodeCursor()
    {
        var stringCursor = Encoding.UTF8.GetString(Convert.FromBase64String(Cursor));
        return new Cursor
        {
            Id = Guid.Parse(stringCursor)
        };
    }
}