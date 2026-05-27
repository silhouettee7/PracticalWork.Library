namespace PracticalWork.Library.Dtos;

public class BorrowBookStatisticDto
{
    public int BorrowedCount { get; set; } 
    public int ReturnedCount {get; set; }
    public int OverdueCount {get; set; }
}