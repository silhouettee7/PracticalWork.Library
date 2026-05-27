using System.Text.Json;
using Domain.Events;
using Domain.Models;

namespace PracticalWork.Report.Models;

/// <summary>
/// Запись активности системы
/// </summary>
public class ActivityLog: ICursor
{
    /// <summary>
    /// Тип события системы
    /// </summary>
    public string EventType { get; set; }
    /// <summary>
    /// Дата события системы
    /// </summary>
    public DateTime EventDate { get; set; }
    /// <summary>
    /// Объект события системы
    /// </summary>
    public BaseEvent Event { get; set; }
    
    /// <summary>
    /// Сериализация объекта события системы
    /// </summary>
    /// <returns>json</returns>
    public string SerializeEvent()
    {
        return JsonSerializer.Serialize(Event);
    }
    
    /// <summary>
    /// Десериализует json в объект события системы
    /// </summary>
    /// <param name="eventType">тип события системы</param>
    /// <param name="jsonb">сериализованный объект события системы</param>
    /// <returns>объект события системы</returns>
    /// <exception cref="JsonException">Исключения если не нашелся подходящий тип события</exception>
    public static BaseEvent DeserializeEvent(string eventType, string jsonb)
    {
        return eventType switch
        {
            "book.created" => JsonSerializer.Deserialize<BookCreatedEvent>(jsonb),
            "book.archived" => JsonSerializer.Deserialize<BookArchivedEvent>(jsonb),
            "book.borrowed" => JsonSerializer.Deserialize<BookBorrowedEvent>(jsonb),
            "book.returned" => JsonSerializer.Deserialize<BookReturnedEvent>(jsonb),
            "reader.created" => JsonSerializer.Deserialize<ReaderCreatedEvent>(jsonb),
            "reader.closed" => JsonSerializer.Deserialize<ReaderClosedEvent>(jsonb),
            _ => throw new JsonException("Unknown event")
        };
    }
    
    /// <summary>
    /// Объект курсорной пагинации
    /// </summary>
    public Cursor Cursor { get; set; }
}