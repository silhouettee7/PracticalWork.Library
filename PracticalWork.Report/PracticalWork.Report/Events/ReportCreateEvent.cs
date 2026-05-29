using Domain.Events;
using ReportStatus = PracticalWork.Report.Enums.ReportStatus;

namespace PracticalWork.Report.Events;

public sealed record ReportCreateEvent(
    Guid Id, DateOnly? PeriodFrom, DateOnly? PeriodTo, 
    IReadOnlyList<string> EventTypes, ReportStatus Status):
    BaseEvent(Guid.NewGuid(), "report.create", "report-service");