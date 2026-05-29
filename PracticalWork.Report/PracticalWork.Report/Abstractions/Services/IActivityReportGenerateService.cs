using PracticalWork.Report.Models;

namespace PracticalWork.Report.Abstractions.Services;

/// <summary>
/// Сервис отвечает за генерацию отчетов
/// </summary>
public interface IActivityReportGenerateService
{
    /// <summary>
    /// Генерирует отчет по записям событий системы
    /// </summary>
    /// <param name="reportId">индентификатор отчета</param>
    /// <param name="logs">записи событий системы</param>
    /// <returns>объект сгенерированного отчета</returns>
    ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs);
}