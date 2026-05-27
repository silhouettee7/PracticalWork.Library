using PracticalWork.Library.Events;

namespace PracticalWork.Library.Contracts.v1.Reports.Response;

/// <summary>
/// Запись события системы
/// </summary>
/// <param name="Event">объект события</param>
/// <param name="EventType">тип события</param>
/// <param name="EventDate">дата события</param>
public record ActivityLogResponse(BaseEvent Event, string EventType, DateTime EventDate);