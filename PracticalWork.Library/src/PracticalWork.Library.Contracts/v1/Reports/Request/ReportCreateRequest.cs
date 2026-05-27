namespace PracticalWork.Library.Contracts.v1.Reports.Request;

/// <summary>
/// Создание отчета
/// </summary>
/// <param name="PeriodFrom">фильтр на дату начала</param>
/// <param name="PeriodTo">фильтр на дату окончания</param>
/// <param name="EventTypes">фильтр на типы событий</param>
public record ReportCreateRequest(
    DateOnly PeriodFrom, 
    DateOnly PeriodTo, 
    IReadOnlyList<string> EventTypes);