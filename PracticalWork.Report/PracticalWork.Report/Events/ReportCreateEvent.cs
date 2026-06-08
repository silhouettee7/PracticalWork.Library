using Domain.Events;
using ReportStatus = PracticalWork.Report.Enums.ReportStatus;

namespace PracticalWork.Report.Events;

/// <summary>
/// Ивент для создания отчета в очередь
/// </summary>
/// <param name="Id">идентификатор</param>
/// <param name="PeriodFrom">с какой даты</param>
/// <param name="PeriodTo">по какую дату</param>
/// <param name="EventTypes">типы событий</param>
/// <param name="Status">статус</param>
public sealed record ReportCreateEvent(
    Guid Id, DateOnly? PeriodFrom, DateOnly? PeriodTo, 
    IReadOnlyList<string> EventTypes, ReportStatus Status):
    BaseEvent(Guid.NewGuid(), "report.create", "report-service");