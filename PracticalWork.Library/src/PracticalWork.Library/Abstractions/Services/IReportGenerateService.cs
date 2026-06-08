using PracticalWork.Library.Models;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис генерации отчетов
/// </summary>
public interface IReportGenerateService
{
    /// <summary>
    /// Сгенерировать отчет в csv
    /// </summary>
    /// <param name="items">строки</param>
    /// <param name="fileName">название файла</param>
    /// <typeparam name="T">тип объекта, от этого зависят данные в строке</typeparam>
    /// <returns>результат генерации отчета</returns>
    ReportGenerateResult GenerateReport<T>(IEnumerable<T> items, string fileName);
    /// <summary>
    /// Сгенерировать отчет для администрации
    /// </summary>
    /// <param name="reportName">название отчета</param>
    /// <param name="booksStatistic">данные для отчета</param>
    /// <returns>результат генерации отчета</returns>
    ReportGenerateResult GenerateReportForAdministration(string reportName, BooksStatistic booksStatistic);
    /// <summary>
    /// Сгенерировать отчет по архивации
    /// </summary>
    /// <param name="reportName">название отчета</param>
    /// <param name="archiveLog">данные для отчета</param>
    /// <returns>результат генерации отчета</returns>
    ReportGenerateResult GenerateArchiveReport(string reportName, ArchiveLog archiveLog);
}