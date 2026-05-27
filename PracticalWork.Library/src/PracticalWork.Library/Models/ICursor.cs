namespace PracticalWork.Library.Models;

/// <summary>
/// Интерфейс должны реализовывать модели с пагинацией
/// </summary>
public interface ICursor
{
    /// <summary>
    /// Объект курсора
    /// </summary>
    public Cursor Cursor { get; set; }
}