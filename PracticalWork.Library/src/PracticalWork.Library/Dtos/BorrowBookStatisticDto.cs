namespace PracticalWork.Library.Dtos;

/// <summary>
/// Статистика о выданных книгах
/// </summary>
public class BorrowBookStatisticDto
{
    /// <summary>
    /// кол-во выданных
    /// </summary>
    public int BorrowedCount { get; set; } 
    /// <summary>
    /// кол-во возвращенных
    /// </summary>
    public int ReturnedCount {get; set; }
    /// <summary>
    /// кол-во возвращенных с просрочкой
    /// </summary>
    public int OverdueCount {get; set; }
}