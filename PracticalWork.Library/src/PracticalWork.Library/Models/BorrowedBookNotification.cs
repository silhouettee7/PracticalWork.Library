namespace PracticalWork.Library.Models;

/// <summary>
/// Уведомление о выданной книге
/// </summary>
public class BorrowedBookNotification
{
    /// <summary>
    /// Имя читателя
    /// </summary>
    public string ReaderFullName { get; set; }
    /// <summary>
    /// Название книги
    /// </summary>
    public string BookTitle { get; set; }
    /// <summary>
    /// Авторы
    /// </summary>
    public IReadOnlyCollection<string> Authors { get; set; }
    /// <summary>
    /// Дата возвращения
    /// </summary>
    public DateOnly ReturnDate { get; set; }
    /// <summary>
    /// Дней до возвращения
    /// </summary>
    public byte DaysCountBeforeReturn { get; set; }
}