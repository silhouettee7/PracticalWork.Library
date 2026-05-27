namespace PracticalWork.Report.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных об отчетах
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Создать отчет
    /// </summary>
    /// <param name="report">объект отчета</param>
    /// <returns>идентификатор отчета</returns>
    Task<Guid> CreateReport(Models.Report report);
    /// <summary>
    /// Получить готовые отчеты
    /// </summary>
    /// <returns>список отчетов</returns>
    Task<IReadOnlyList<Models.Report>> GetReadyReports();
    /// <summary>
    /// Получить отчет по идентификатору
    /// </summary>
    /// <param name="reportId">идентификатор отчета</param>
    /// <returns>объект отчета</returns>
    Task<Models.Report> GetReportById(Guid reportId);
    /// <summary>
    /// Получить отчет по имени файла
    /// </summary>
    /// <param name="reportName">название отчета</param>
    /// <returns>идентификатор и объект отчета</returns>
    Task<(Guid id, Models.Report report)> GetReportByName(string reportName);
    /// <summary>
    /// Обновить информациб об отчете
    /// </summary>
    /// <param name="reportId">идентификатор отчета</param>
    /// <param name="report">объект отчета</param>
    /// <returns>задача</returns>
    Task UpdateReport(Guid reportId,Models.Report report);
    Task SaveReportAsync(Models.Report report, CancellationToken cancellationToken);
}