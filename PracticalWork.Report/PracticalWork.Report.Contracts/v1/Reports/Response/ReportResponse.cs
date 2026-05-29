namespace PracticalWork.Report.Contracts.v1.Reports.Response;

/// <summary>
/// Отчет
/// </summary>
/// <param name="ReportName">название файла отчета</param>
/// <param name="FilePath">ссылка на файл отчета</param>
/// <param name="PeriodFrom">фильтр даты начала</param>
/// <param name="PeriodTo">фильтр даты окончания</param>
/// <param name="EventTypes">фильтр типов событий</param>
/// <param name="GeneratedAt">когда создан</param>
public record ReportResponse(
    string ReportName, 
    string FilePath,
    DateOnly? PeriodFrom, 
    DateOnly? PeriodTo, 
    IReadOnlyList<string> EventTypes,
    DateTime GeneratedAt);