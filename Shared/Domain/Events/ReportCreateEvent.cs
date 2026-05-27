using Domain.Enums;

namespace Domain.Events;

public sealed record ReportCreateEvent(
    Guid Id, DateOnly? PeriodFrom, DateOnly? PeriodTo, 
    IReadOnlyList<string> EventTypes, ReportStatus Status):
    BaseEvent(Guid.NewGuid(), "report.create", "report-service");