using PracticalWork.Library.Models;

namespace PracticalWork.Library.Dtos;

/// <summary>
/// Детали книги
/// </summary>
public class BookDetailsDto
{
    /// <summary>
    /// Идентификатор книги
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Объект книги
    /// </summary>
    public Book Book { get; set; }
}