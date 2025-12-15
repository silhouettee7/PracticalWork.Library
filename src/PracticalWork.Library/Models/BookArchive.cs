namespace PracticalWork.Library.Models;

/// <summary>
/// Архивация книги
/// </summary>
public class BookArchive
{
    /// <summary>
    /// идентификатор книги
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// название книги
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// дата архивирования
    /// </summary>
    public DateTime ArchivedAt { get; set; }
}