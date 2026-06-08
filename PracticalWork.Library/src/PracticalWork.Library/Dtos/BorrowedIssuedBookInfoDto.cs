namespace PracticalWork.Library.Dtos;

/// <summary>
/// информация о выданный книге
/// </summary>
public class BorrowedIssuedBookInfoDto
{
    /// <summary>
    /// Идентификатор книги
    /// </summary>
    public Guid Id { get; set; }
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
    public IReadOnlyList<string> Authors { get; set; }
    /// <summary>
    /// Дата выдачи
    /// </summary>
    public DateOnly DueDate { get; set; }
}