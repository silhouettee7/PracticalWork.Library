namespace PracticalWork.Library.Dtos;

/// <summary>
/// Свободная старая книга
/// </summary>
public class AvailableOldBookDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Название
    /// </summary>
    public string Title { get; set; }
}