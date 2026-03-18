using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис отвечает за генерацию отчетов
/// </summary>
public interface IReportGenerateService
{
    /// <summary>
    /// Генерирует отчет по записям событий системы
    /// </summary>
    /// <param name="reportId">индентификатор отчета</param>
    /// <param name="logs">записи событий системы</param>
    /// <returns>объект сгенерированного отчета</returns>
    ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs);
    ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string reportName);
}